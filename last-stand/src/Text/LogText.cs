using LastStand.Combat;
using LastStand.Waves;

namespace LastStand.Text;

internal static class LogText
{
	public const string Unloaded = "unloaded.";
	public const string ModeOptionFailed = "could not add the Last Stand game mode option.";
	public const string SoloButtonFailed = "could not add the Last Stand solo button.";
	public const string TickFailed = "session tick failed; the session was stopped.";
	public const string HudFailed = "HUD label update failed; the stock label was restored.";
	public const string SessionStopped = "session stopped.";
	public const string NotHosting = "only the host of a Last Stand run can do that.";

	public const string GoalRemoved = "removed the run's Shape of Dreams goal; Last Stand has no destination.";

	public const string TestBossAlreadyAlive = "test boss: one is already alive.";
	public const string TestBossNoHero = "test boss: no living host hero to spawn near.";
	public const string TestBossNotInArena = "test boss: only works while fighting in the arena.";

	public static string HeroicBossUnknown(string typeName)
	{
		return $"heroic boss: no boss named {typeName}.";
	}

	public static string HeroicBossUnsupported(string typeName)
	{
		return $"heroic boss: {typeName} is hidden or runs its own death logic (it may end the run); skipped.";
	}

	public static string TestBossSpawned(string typeName, int level)
	{
		return $"test boss: spawned {typeName} at level {level}.";
	}

	public static string HeroicBossDied(int chaosRewards)
	{
		return $"heroic boss: died, dropped {chaosRewards} legendary chaos reward(s).";
	}

	public static string PainZoneStarted()
	{
		return "pain zone: Aura of Pain environment added to the arena.";
	}

	public static string ConfigReset(int schemaVersion, string backupPath)
	{
		return $"saved config is from an older version; reset to version {schemaVersion} defaults. " +
		       $"Old file kept at {backupPath}.";
	}

	public static string Loaded(string name, string version)
	{
		return $"loaded: {name} v{version}.";
	}

	public static string ArenaRoomApplied(string room, int nodes)
	{
		return $"arena room '{room}' applied to {nodes} node(s).";
	}

	public static string PlainArenaPrepared(string room, int triggers)
	{
		return $"'{room}' loads cleared; {triggers} entry trigger(s) disabled.";
	}

	public static string ArenaReady(int heroes, UnityEngine.Vector3 center)
	{
		return $"arena ready: {heroes} hero(es) staged at {center}.";
	}

	public static string WaveStarted(WavePlan plan)
	{
		return $"wave {plan.Number}: regular={plan.RegularCount} surge={plan.IsSurge} hunter={plan.HunterCount} " +
		       $"miniboss={plan.MiniBossCount} pain={plan.PainCount} heroic={plan.HeroicBossCount} " +
		       $"ambient={plan.AmbientLevel} " +
		       $"health=+{plan.HealthBonusPercent:0}% damage=+{plan.DamageBonusPercent:0}% " +
		       $"foreign={plan.ForeignTypesUnlocked} mirage={plan.MirageSkinsUnlocked} " +
		       $"huntLevel={plan.HuntLevel} cap={plan.MaxAlive}.";
	}

	public static string CatalogBuilt(int foreignTypes, int mirageSkins, int hunterStartWave, int heroicBosses)
	{
		return $"catalog: {foreignTypes} monster type(s) from other worlds, {mirageSkins} Mirage skin(s), " +
		       $"{heroicBosses} heroic boss type(s); hunters start at wave {hunterStartWave}.";
	}

	public static string ModifiersStripped(int count)
	{
		return $"removed {count} room modifier(s) from arena nodes.";
	}

	public static string ModifierBlocked(string modifierType, int nodeIndex)
	{
		return $"blocked room modifier {modifierType} on arena node {nodeIndex}.";
	}

	public static string WaveCleared(int wave)
	{
		return $"wave {wave} cleared.";
	}

	public static string CampRiftDirected(int node)
	{
		return $"camp rift now leads straight to node {node}.";
	}

	public static string SpawnSkipped(EnemyTier tier)
	{
		return $"could not spawn a {tier}; skipped.";
	}

	public static string WellPlaced(UnityEngine.Vector3 position)
	{
		return $"Dream Well placed at {position}.";
	}

	public static string Status(bool selected, bool server, string phase, int wave, bool inProgress,
		double secondsToNext, int occupied, int pending, string items)
	{
		return $"mode={selected} server={server} phase={phase} wave={wave} inProgress={inProgress} " +
		       $"next={secondsToNext:0}s " +
		       $"alive={occupied} pending={pending} {items}.";
	}

	public const string GuaranteedDropSource = "WaveClear";

	public static string ItemDropped(int wave, string source, Rarity? rolled, Rarity dropped, bool memory)
	{
		string kind = memory ? "memory" : "essence";
		string rolledText = rolled.HasValue ? rolled.Value.ToString() : "game";
		string fallback = rolled.HasValue && rolled.Value != dropped ? $" (rolled {rolledText}, none in pool)" : string.Empty;
		return $"loot: wave {wave} {source} dropped a {dropped} {kind}{fallback}.";
	}

	public static string ItemTally(int common, int rare, int epic, int legendary)
	{
		return $"items=common:{common} rare:{rare} epic:{epic} legendary:{legendary}";
	}
}
