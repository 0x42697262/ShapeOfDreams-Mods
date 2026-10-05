using System.Collections.Generic;
using LastStand.Combat;
using LastStand.Game;
using UnityEngine;

namespace LastStand.Loot;

internal sealed class LootService
{
	private readonly DropLedger _ledger = new DropLedger();
	private readonly LootExpiry _expiry = new LootExpiry();
	private readonly List<DewPlayer> _players = new List<DewPlayer>();
	private readonly List<string> _guids = new List<string>();

	public int TrackedItems => _expiry.TrackedCount;

	public void OnEnemyDied(EnemyDeath death, int wave, double now, LootSettings settings)
	{
		LootDecision decision = LootRules.Decide(settings.For(death.Tier), settings, wave, Random.value);

		LootDropper.DropDreamDust(death.Position, decision.DreamDust);
		LootDropper.DropGold(death.Position, decision.Gold);

		if (!decision.DropsItem)
		{
			return;
		}

		bool fromMob = death.Tier.IsMob();
		DewPlayer recipient = fromMob
			? ChooseMobRecipient(death.Killer, settings.MobItemsPerPlayerPerWave)
			: ChooseRecipient(death.Killer);

		if (fromMob && recipient == null)
		{
			return;
		}

		bool memory = LootRules.ChooseMemory(fromMob, PlayerRoster.PlayerCount(), _ledger.MobMemories,
			_ledger.MobEssences, settings.MemoryShare, Random.value);

		DropItem(death.Position, recipient, memory, decision.ItemRarity, fromMob, now, settings);
	}

	public void OnWaveStarted()
	{
		_ledger.StartWave();
	}

	public void OnWaveCleared(double now, LootSettings settings)
	{
		if (settings.GuaranteedDropPerWave)
		{
			GrantMissedDrops(now, settings);
		}
	}

	public void Tick(double now)
	{
		_expiry.Tick(now);
	}

	public void Reset()
	{
		_ledger.StartWave();
		_expiry.Clear();
	}

	private void GrantMissedDrops(double now, LootSettings settings)
	{
		PlayerRoster.FillPlayersWithLiveHeroes(_players);
		foreach (DewPlayer player in _players)
		{
			if (_ledger.HasDropThisWave(player.guid))
			{
				continue;
			}

			bool memory = LootRules.ChooseMemory(false, PlayerRoster.PlayerCount(), 0, 0, settings.MemoryShare,
				Random.value);
			DropItem(player.hero.agentPosition, player, memory, null, false, now, settings);
		}
	}

	private DewPlayer ChooseMobRecipient(DewPlayer killer, int perPlayerCap)
	{
		FillCandidates();
		return Find(_ledger.ChooseMobRecipient(_guids, GuidOf(killer), perPlayerCap));
	}

	private DewPlayer ChooseRecipient(DewPlayer killer)
	{
		FillCandidates();
		return _players.Count == 0 ? killer : Find(_ledger.ChooseRecipient(_guids, GuidOf(killer))) ?? killer;
	}

	private void FillCandidates()
	{
		PlayerRoster.FillPlayersWithLiveHeroes(_players);
		_guids.Clear();
		foreach (DewPlayer player in _players)
		{
			_guids.Add(player.guid);
		}
	}

	private DewPlayer Find(string guid)
	{
		if (guid == null)
		{
			return null;
		}

		foreach (DewPlayer player in _players)
		{
			if (player.guid == guid)
			{
				return player;
			}
		}

		return null;
	}

	private static string GuidOf(DewPlayer player)
	{
		return player != null ? player.guid : null;
	}

	private void DropItem(Vector3 position, DewPlayer recipient, bool memory, Rarity? rarity, bool fromMob,
		double now, LootSettings settings)
	{
		Actor item = memory
			? LootDropper.DropMemory(position, recipient, rarity)
			: LootDropper.DropEssence(position, recipient, rarity);

		if (item == null)
		{
			return;
		}

		if (recipient != null)
		{
			_ledger.Record(recipient.guid, fromMob, memory);
		}

		_expiry.Track(item, now + settings.DespawnSeconds);
	}
}
