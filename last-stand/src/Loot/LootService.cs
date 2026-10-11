using System.Collections.Generic;
using LastStand.Combat;
using LastStand.Game;
using LastStand.Text;
using LastStand.Waves;
using UnityEngine;

namespace LastStand.Loot;

internal sealed class LootService
{
	private readonly DropLedger _ledger = new DropLedger();

	private readonly DropTally _tally = new DropTally();

	private readonly List<DewPlayer> _players = new List<DewPlayer>();

	private readonly List<string> _guids = new List<string>();

	public void OnEnemyDied(EnemyDeath death, int wave, double mergeMultiplier, LootSettings settings)
	{
		double currency = death.Tier.IsMob() ? mergeMultiplier : MergeRule.NoChange;
		LootDecision decision = LootRules.Decide(settings.For(death.Tier), settings, wave, Random.value, currency);

		LootDropper.DropDreamDust(death.Position, decision.DreamDust);
		LootDropper.DropGold(death.Position, decision.Gold);

		if (!settings.ItemDrops || !decision.DropsItem)
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

		DropItem(death.Position, recipient, memory, decision.ItemRarity, fromMob, wave, death.Tier.ToString());
	}

	public void OnWaveStarted()
	{
		_ledger.StartWave();
	}

	public void OnWaveCleared(int wave, LootSettings settings)
	{
		if (settings.ItemDrops && settings.GuaranteedDropPerWave)
		{
			GrantMissedDrops(wave, settings);
		}

		if (settings.FreeLeftoverItems)
		{
			FreeLeftovers(wave);
		}
	}

	public string DescribeTally()
	{
		return LogText.ItemTally(_tally.Of(Rarity.Common), _tally.Of(Rarity.Rare), _tally.Of(Rarity.Epic),
			_tally.Of(Rarity.Legendary));
	}

	public void Reset()
	{
		_ledger.StartWave();
		_tally.Clear();
	}

	private static void FreeLeftovers(int wave)
	{
		int freed = LeftoverItems.Release();
		if (freed == 0)
		{
			return;
		}

		Log.Info(LogText.LeftoverItemsFreed(wave, freed));
		if (PlayerRoster.PlayerCount() > 1)
		{
			ChatNotices.Broadcast(Messages.LeftoverItemsFreed(freed));
		}
	}

	private void GrantMissedDrops(int wave, LootSettings settings)
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
			DropItem(player.hero.agentPosition, player, memory, null, false, wave, LogText.GuaranteedDropSource);
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
		int wave, string source)
	{
		Actor item = memory
			? LootDropper.DropMemory(position, recipient, rarity, out Rarity dropped)
			: LootDropper.DropEssence(position, recipient, rarity, out dropped);

		if (item == null)
		{
			return;
		}

		_tally.Add(dropped);
		Log.Info(LogText.ItemDropped(wave, source, rarity, dropped, memory));

		if (recipient != null)
		{
			_ledger.Record(recipient.guid, fromMob, memory);
		}
	}
}
