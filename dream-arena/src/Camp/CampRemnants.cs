using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Text;
using UnityEngine;

namespace DreamArena.Camp;

internal sealed class CampRemnants
{
	private readonly Dictionary<string, HashSet<string>> _taken = new Dictionary<string, HashSet<string>>();
	private int _builtNode = ArenaLocator.NoNode;
	private int _attemptNode = ArenaLocator.NoNode;
	private int _attempts;
	private double _nextAttemptAt;

	public void Tick(double now)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.isInAnyTransition)
		{
			return;
		}

		int node = zones.currentNodeIndex;
		if (node == _builtNode)
		{
			return;
		}

		if (node != _attemptNode)
		{
			_attemptNode = node;
			_attempts = 0;
			_nextAttemptAt = 0;
		}

		if (_attempts >= Tuning.RemnantMaxAttempts || now < _nextAttemptAt)
		{
			return;
		}

		_attempts++;
		_nextAttemptAt = now + Tuning.RemnantRetrySeconds;
		if (AlreadyStanding() || Build())
		{
			_builtNode = node;
		}
		else if (_attempts >= Tuning.RemnantMaxAttempts)
		{
			Log.Warn(LogText.RemnantsGaveUp(node, Tuning.RemnantMaxAttempts));
		}
	}

	public bool HasTaken(DewPlayer player, string item)
	{
		return _taken.TryGetValue(player.guid, out HashSet<string> taken) && taken.Contains(item);
	}

	public void RecordTaken(DewPlayer player, string item)
	{
		if (!_taken.TryGetValue(player.guid, out HashSet<string> taken))
		{
			taken = new HashSet<string>();
			_taken[player.guid] = taken;
		}

		taken.Add(item);
	}

	public void Restock()
	{
		_taken.Clear();
		_builtNode = ArenaLocator.NoNode;
		_attemptNode = ArenaLocator.NoNode;
		_attempts = 0;
	}

	public void Reset()
	{
		Restock();
		_nextAttemptAt = 0;
	}

	private static bool AlreadyStanding()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return false;
		}

		foreach (Actor actor in actors.allActors)
		{
			if (actor is ChoiceShrine remnant && !remnant.IsNullOrInactive() && RemnantTag.TryRead(remnant, out _))
			{
				return true;
			}
		}

		return false;
	}

	private static bool Build()
	{
		Shrine_Retrospection memory = DewResources.GetByType<Shrine_Retrospection>();
		Shrine_Enlightenment essence = DewResources.GetByType<Shrine_Enlightenment>();
		if (memory == null || essence == null)
		{
			Log.Warn(LogText.RemnantPrefabsMissing);
			return false;
		}

		if (!CampLayout.TryFind(out Vector3 center, out Vector3 opening))
		{
			return false;
		}

		int rarities = Tuning.RemnantRarities.Length;
		int count = rarities * Tuning.RemnantKinds;
		if (!RemnantRing.TrySolve(center, opening, count, out Vector3[] slots, out Vector3[] facings))
		{
			Log.Warn(LogText.RemnantRingBlocked(count));
			return false;
		}

		int built = 0;
		for (int i = 0; i < count; i++)
		{
			bool isMemory = i < rarities;
			Rarity rarity = Tuning.RemnantRarities[isMemory ? i : count - 1 - i];
			Quaternion facing = Quaternion.LookRotation(facings[i], Vector3.up);
			ChoiceShrine remnant = isMemory
				? Place(memory, RemnantKind.Memory, rarity, slots[i], facing)
				: Place(essence, RemnantKind.Essence, rarity, slots[i], facing);
			if (remnant != null)
			{
				built++;
			}
		}

		Log.Info(LogText.RemnantsBuilt(built, center));
		return built > 0;
	}

	private static ChoiceShrine Place<T>(T prefab, RemnantKind kind, Rarity rarity, Vector3 position,
		Quaternion rotation) where T : ChoiceShrine
	{
		return Dew.CreateActor(prefab, position, rotation, null, created =>
		{
			Configure(created);
			RemnantTag.Write(created, kind, rarity);
		});
	}

	private static void Configure(ChoiceShrine remnant)
	{
		remnant.maxTotalUseCount = -1;
		remnant.maxUseCountPerPlayer = -1;
		remnant.useCooldown = 0f;
		remnant.baseGoldCost = 0;
		remnant.unavailableByDefault = false;
		remnant.interactableDelay = 0f;
	}
}
