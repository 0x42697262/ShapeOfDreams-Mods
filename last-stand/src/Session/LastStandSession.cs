using System;
using LastStand.Arena;
using LastStand.Combat;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Loot;
using LastStand.Mode;
using LastStand.Text;
using LastStand.Waves;
using LastStand.Well;
using Mirror;

namespace LastStand.Session;

internal sealed class LastStandSession
{
	private readonly EnemyRegistry _enemies = new EnemyRegistry();

	private readonly ArenaArrival _arrival = new ArenaArrival();

	private readonly CampRift _campRift = new CampRift();

	private readonly RunGoal _goal = new RunGoal();

	private readonly MidJoinGate _midJoin = new MidJoinGate();

	private readonly WaveTracker _tracker = new WaveTracker();

	private readonly StartingWorld _world = new StartingWorld();

	private readonly GoldPrices _goldPrices = new GoldPrices();

	private readonly CurseStart _curseStart = new CurseStart();

	private readonly ArenaPanel _panel = new ArenaPanel();

	private readonly StartingBonus _startingBonus = new StartingBonus();

	private readonly HeroicBosses _heroicBosses = new HeroicBosses();

	private readonly TestBoss _testBoss;

	private readonly LootService _loot = new LootService();

	private readonly WaveDirector _waves;

	private readonly PainService _pain;

	private SessionSettings _settings;

	private SessionPhase _phase = SessionPhase.Inactive;

	private int _arenaNode = ArenaLocator.NoNode;

	private double _now;

	public PlainArena PlainArena { get; } = new PlainArena();

	public ArenaShrines Shrines { get; } = new ArenaShrines();

	public SessionSettings Settings => _settings;

	public LastStandSession(SessionSettings settings)
	{
		_settings = settings;
		_testBoss = new TestBoss(_heroicBosses);
		_waves = new WaveDirector(_enemies, _heroicBosses, OnEnemySpawned);
		_pain = new PainService(_enemies, _waves.HasPending);

		_waves.WaveStarted += OnWaveStarted;
		_waves.WaveCleared += OnWaveCleared;
		_waves.FastCleared += OnFastCleared;
		_enemies.Died += OnEnemyDied;
		_pain.GroupFell += _waves.RestoreHuntedZone;
	}

	public bool OwnsPainZone(RoomModifierBase modifier)
	{
		return _pain.OwnsZone(modifier);
	}

	public void UseSettings(SessionSettings settings)
	{
		_settings = settings;
		if (_phase == SessionPhase.Fighting)
		{
			ApplyGoldPrices(_waves.CurrentWave);
		}
	}

	public void Tick(double now)
	{
		_now = now;

		if (!LastStandMode.IsHosting())
		{
			if (_phase != SessionPhase.Inactive)
			{
				Reset();
			}

			return;
		}

		_goal.Tick(now);
		_tracker.Tick(now, TrackerProgress(now));
		_startingBonus.Tick(_settings.StartingGold, _settings.StartingDreamDust);

		if (!ArenaLocator.IsStandingInArena())
		{
			if (_phase == SessionPhase.Fighting || _phase == SessionPhase.Arriving)
			{
				StopFight();
			}

			_phase = SessionPhase.Camp;
			_world.Tick(now, _settings.StartingWorld);
			_campRift.Tick(now);
			return;
		}

		int node = ArenaLocator.CurrentNode();
		if (_phase != SessionPhase.Fighting || node != _arenaNode)
		{
			Arrive(now, node);
			return;
		}

		_waves.Tick(now, _settings.Waves);
		_midJoin.Tick(now);
		TickCurseStart(now);
		_pain.Tick(now, _settings.Pain);
		_panel.Tick(_waves.HasArenaModifier || _pain.ZoneActive);
		Shrines.Tick(now, _waves.ClearedWaves, _settings.AscensionResetWaves, _settings.Waves.CurseToStart);
	}

	public void SpawnTestBoss(string typeName)
	{
		if (_phase != SessionPhase.Fighting)
		{
			Log.Warn(LogText.TestBossNotInArena);
			return;
		}

		_testBoss.Spawn(typeName);
	}

	public bool OwnsHeroicBoss(Entity entity)
	{
		return _heroicBosses.Owns(entity);
	}

	public void GiveGold(int amount)
	{
		int players = PlayerRoster.GiveGold(amount);
		if (players == 0)
		{
			Log.Warn(LogText.GoldNotGranted);
			return;
		}

		Log.Info(LogText.GoldGranted(amount, players));
	}

	public void GiveDreamDust(int amount)
	{
		int players = PlayerRoster.GiveDreamDust(amount);
		if (players == 0)
		{
			Log.Warn(LogText.DreamDustNotGranted);
			return;
		}

		Log.Info(LogText.DreamDustGranted(amount, players));
	}

	public void SetHeroLevel(int level)
	{
		Log.Info(LogText.LevelSet(level, PlayerRoster.SetHeroLevel(level)));
	}

	public void GrantSecondWind(int charges, float invulnerableSeconds)
	{
		float seconds = Math.Max(0f, invulnerableSeconds);
		Log.Info(LogText.SecondWindSet(charges, seconds, SecondWind.Grant(charges, seconds)));
	}

	public void SkipWaves(int waves)
	{
		if (_phase != SessionPhase.Fighting || waves <= 0)
		{
			Log.Warn(LogText.SkipNotPossible);
			return;
		}

		_pain.Reset();
		_waves.Skip(waves);
		Log.Info(LogText.SkipQueued(waves, _waves.CurrentWave));
	}

	public void DropEssence(string query, int? quality)
	{
		if (!TryGetHostHero(out Hero host)
		    || !TryResolve(CheatItems.FindEssence(query), CommandText.EssenceKind, query, out ItemLookup item))
		{
			return;
		}

		Gem gem = CheatItems.SpawnEssence(item, quality, host.owner, host.agentPosition, out int spawned);
		LogDrop(gem, CommandText.EssenceKind, query, CommandText.QualityStat, spawned);
	}

	public void DropMemory(string query, int? level)
	{
		if (!TryGetHostHero(out Hero host)
		    || !TryResolve(CheatItems.FindMemory(query), CommandText.MemoryKind, query, out ItemLookup item))
		{
			return;
		}

		SkillTrigger skill = CheatItems.SpawnMemory(item, level, host.owner, host.agentPosition, out int spawned);
		LogDrop(skill, CommandText.MemoryKind, query, CommandText.LevelStat, spawned);
	}

	private static bool TryGetHostHero(out Hero host)
	{
		host = DewPlayer.local != null ? DewPlayer.local.hero : null;
		if (!host.IsNullInactiveDeadOrKnockedOut())
		{
			return true;
		}

		Log.Warn(LogText.CheatNoHostHero);
		return false;
	}

	private static bool TryResolve(ItemLookup lookup, string kind, string query, out ItemLookup item)
	{
		item = lookup;
		if (lookup.IsFound)
		{
			return true;
		}

		Log.Warn(lookup.Candidates.Count == 0
			? LogText.ItemNotFound(kind, query)
			: LogText.ItemAmbiguous(kind, query, lookup.Candidates, Tuning.MaxListedCandidates));
		return false;
	}

	private static void LogDrop(Actor dropped, string kind, string query, string stat, int value)
	{
		switch (dropped)
		{
			case Gem gem:
				Log.Info(LogText.ItemSpawned(kind, gem.GetType().Name, gem.rarity, stat, value));
				return;
			case SkillTrigger skill:
				Log.Info(LogText.ItemSpawned(kind, skill.GetType().Name, skill.rarity, stat, value));
				return;
			default:
				Log.Warn(LogText.ItemNotFound(kind, query));
				return;
		}
	}

	public int CapKillGold(int amount)
	{
		return KillGoldRule.Cap(amount, _settings.MaxKillGoldPerPlayer, PlayerRoster.GoldSharers());
	}

	public void OnHeroicBossDied(BossMonster boss)
	{
		_heroicBosses.OnDied(boss, _settings.Loot.HeroicBossChaosRewards);
	}

	public void ApplyArenaRoom()
	{
		ArenaRoomOverride.Apply(_settings.ArenaRoom, _settings.StripRoomModifiers);
	}

	public bool AllowsModifier(ZoneManager zones, int nodeIndex, string modifierType)
	{
		if (!_settings.StripRoomModifiers || ModifierGate.Allows(zones, nodeIndex, modifierType))
		{
			return true;
		}

		Log.Info(LogText.ModifierBlocked(modifierType, nodeIndex));
		return false;
	}

	public bool AllowsRoomClear()
	{
		if (!RoomCombat.IsHeldOpen)
		{
			return true;
		}

		Log.Info(LogText.RoomClearBlocked);
		return false;
	}

	public void RerollChaos(Shrine_Chaos shrine)
	{
		ChaosRarity.Reroll(shrine, _settings.Loot, Math.Max(Tuning.FirstWave, _waves.CurrentWave));
	}

	public void StartNextWaveNow()
	{
		if (_phase == SessionPhase.Fighting)
		{
			_waves.StartNextWaveNow(_now);
		}
	}

	public void JumpToWave(int wave)
	{
		if (_phase != SessionPhase.Fighting || wave < Tuning.FirstWave)
		{
			Log.Warn(LogText.JumpNotPossible);
			return;
		}

		int previous = wave - 1;
		_waves.Resume(previous, false, _now, _waves.FastClearStacks);
		ProgressStore.Save(_arenaNode, previous, false, _waves.FastClearStacks);
		Log.Info(LogText.JumpedToWave(wave));
	}

	private string TrackerProgress(double now)
	{
		if (!_settings.ShowWaveInQuestTracker || _phase != SessionPhase.Fighting)
		{
			return null;
		}

		if (_waves.WaitingForCurse)
		{
			return TrackerText.AwaitingCurse(_waves.CurrentWave + 1);
		}

		if (_waves.WaveInProgress)
		{
			return TrackerText.WaveRunning(_waves.CurrentWave, _enemies.Occupied(now) + _waves.PendingSpawns);
		}

		return TrackerText.NextWave(_waves.CurrentWave + 1, (int)Math.Ceiling(_waves.SecondsUntilNextWave(now)));
	}

	public string Describe()
	{
		return LogText.Status(LastStandMode.IsSelected(), NetworkServer.active, _phase.ToString(),
			_waves.CurrentWave, _waves.WaveInProgress, _waves.SecondsUntilNextWave(_now), _enemies.Occupied(_now),
			_waves.PendingSpawns, _loot.DescribeTally());
	}

	public void Reset()
	{
		StopFight();
		PlainArena.Reset();
		_campRift.Reset();
		_goal.Reset();
		_midJoin.Reset();
		_tracker.Remove();
		_world.Reset();
		_phase = SessionPhase.Inactive;
	}

	private void Arrive(double now, int node)
	{
		_phase = SessionPhase.Arriving;
		if (!_arrival.TryArrive(now, out ArenaPoints arena))
		{
			return;
		}

		StopFight();
		_arenaNode = node;
		_phase = SessionPhase.Fighting;

		int hidden = WorldMapNodes.HideAllBut(NetworkedManagerBase<ZoneManager>.softInstance, node);
		if (hidden > 0)
		{
			Log.Info(LogText.MapNodesHidden(hidden, node));
		}

		Shrines.Place(arena, _settings);

		if (ProgressStore.TryLoad(node, out int wave, out bool inProgress, out int fastClears))
		{
			int adopted = inProgress ? EnemyAdoption.AdoptSurvivors(_enemies) : 0;
			_waves.Resume(wave, inProgress, now + _settings.Waves.IntermissionSeconds, fastClears);
			ApplyGoldPrices(wave);
			Log.Info(LogText.ProgressRestored(wave, inProgress, adopted, fastClears));
			return;
		}

		_waves.Begin(now + _settings.FirstWaveDelaySeconds);
		ApplyGoldPrices(_waves.CurrentWave);
		Log.Info(LogText.ArenaReady(_arrival.StagedHeroes, arena.Center));
		ChatNotices.Broadcast(_settings.Waves.CurseToStart && Shrines.HatredStanding
			? Messages.ArenaReadyAwaitingCurse()
			: Messages.ArenaReady((int)Math.Ceiling(_settings.FirstWaveDelaySeconds)));
	}

	private void StopFight()
	{
		_waves.Reset();
		_pain.Reset();
		_panel.Reset();
		_loot.Reset();
		_enemies.Clear();
		_arrival.Reset();
		_testBoss.Forget();
		_heroicBosses.Clear();
		Shrines.Forget();
		_goldPrices.Restore();
		_curseStart.Reset();
		_arenaNode = ArenaLocator.NoNode;
	}

	private void OnEnemySpawned(Entity entity, EnemyTier tier)
	{
		_enemies.Track(entity, tier);
		if (tier == EnemyTier.Pain)
		{
			_pain.OnSpawned(entity, _settings.Pain);
		}
	}

	private void OnWaveStarted(WavePlan plan)
	{
		ProgressStore.Save(_arenaNode, plan.Number, true, _waves.FastClearStacks);
		_loot.OnWaveStarted();
		Shrines.OnWaveStarted();
		ApplyGoldPrices(plan.Number);
	}

	private void ApplyGoldPrices(int wave)
	{
		_goldPrices.Set(_settings.GoldPrices.MultiplierAt(wave), wave);
	}

	private void TickCurseStart(double now)
	{
		switch (_curseStart.Tick(now, _settings.Waves, _waves, Shrines.HatredStanding))
		{
			case CurseStartEvent.HoldStarted:
				Shrines.ResetHatredUses();
				Log.Info(LogText.CurseHoldStarted(_waves.CurrentWave + 1));
				break;
			case CurseStartEvent.StartedWithoutCurse:
				Log.Warn(LogText.WaveStartedWithoutCurse(_waves.CurrentWave));
				break;
		}
	}

	public void OnHatredCurse(Hero chooser, HatredStrengthType strength)
	{
		if (_phase != SessionPhase.Fighting || strength == HatredStrengthType.None
		    || !_curseStart.TryRelease(_now, _settings.Waves, _waves))
		{
			return;
		}

		int othersCursed = SharedCurses.CurseOthers(chooser, strength);
		double delay = _settings.Waves.CurseStartDelaySeconds;
		int wave = _waves.CurrentWave + 1;
		Log.Info(LogText.CurseStartsWave(wave, strength.ToString(), othersCursed, delay));
		ChatNotices.Broadcast(Messages.CurseStartsWave(strength.ToString(), wave, (int)Math.Ceiling(delay)));
	}

	public void OnCurseCreated(CurseStatusEffect curse)
	{
		if (_phase != SessionPhase.Fighting || !CurseGoals.EndsByKillsOrTravel(curse))
		{
			return;
		}

		int monsters = _waves.MonstersInWave(_settings.Waves);
		if (monsters <= 0)
		{
			return;
		}

		QuestProgressType before = curse.progressType;
		int beforeAmount = curse.requiredAmount;
		int kills = CurseGoalRule.Kills(monsters, PlayerRoster.PlayerCount(),
			_settings.Curses.WavesFor(curse.currentStrength));
		CurseGoals.SetKills(curse, kills);
		Log.Info(LogText.CurseGoalSet(curse.GetType().Name, before.ToString(), beforeAmount, kills));
	}

	public void OnHatredShardOpening(Shrine_Hatred_Reward shard)
	{
		if (_phase != SessionPhase.Fighting)
		{
			return;
		}

		int wave = Math.Max(Tuning.FirstWave, _waves.CurrentWave);
		float dreamDust = HatredShards.Open(shard, wave, _settings.HatredDreamDustPerWave, _settings.HatredShardGold);
		Log.Info(LogText.HatredShardOpened(wave, dreamDust, _settings.HatredShardGold));
	}

	public void OnShrineUsed(Shrine shrine, Entity user)
	{
		Shrines.OnUsed(shrine, user, _settings.DestinyClaimsPerWave);
	}

	private void OnFastCleared()
	{
		ProgressStore.Save(_arenaNode, _waves.CurrentWave, true, _waves.FastClearStacks);
	}

	private void OnWaveCleared(int wave)
	{
		ProgressStore.Save(_arenaNode, wave, false, _waves.FastClearStacks);
		_loot.OnWaveCleared(wave, _settings.Loot);
		Shrines.OnWaveCleared(wave, _settings.AscensionResetWaves);

		int revived = HeroSouls.ReviveKnockedOut();
		if (revived > 0)
		{
			Log.Info(LogText.HeroesRevived(wave, revived));
			ChatNotices.Broadcast(Messages.HeroesRevived(revived));
		}
	}

	private void OnEnemyDied(EnemyDeath death)
	{
		_loot.OnEnemyDied(death, _waves.CurrentWave, _waves.MergeMultiplier, _settings.Loot);
		_pain.OnEnemyDied(death);
	}
}
