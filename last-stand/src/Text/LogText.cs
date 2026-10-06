using LastStand.Combat;
using LastStand.Waves;

namespace LastStand.Text;

internal static class LogText
{
	public const string Unloaded = "unloaded.";
	public const string ModeOptionFailed = "could not add the Last Stand game mode option.";
	public const string SoloButtonFailed = "could not add the Last Stand solo button.";
	public const string LobbyButtonFailed = "could not add the Last Stand settings button to the lobby; use the laststand_config console command.";
	public const string ConfigSaveFailed = "could not save the Last Stand config.";
	public const string ConfigApplied = "config applied and saved.";
	public const string BossMusicMissing = "boss music: none of the boss tracks could be found; the room's music plays.";
	public const string HeroSoulFailed = "hero soul: could not clear a knocked-out hero's soul.";

	public static string HeroSoulRemoved(int node)
	{
		return $"hero soul: removed one from node {node}; knocked-out heroes are revived at the next wave instead.";
	}

	public static string HeroesRevived(int wave, int count)
	{
		return $"revive: {count} knocked-out hero(es) revived after wave {wave} was cleared.";
	}

	public static string MapNodesHidden(int count, int kept)
	{
		return $"world map: hid {count} node(s); only the camp and the arena (node {kept}) are shown.";
	}

	public static string BossMusicPlaying(string track)
	{
		return $"boss music: playing {track} (host only).";
	}
	public const string TickFailed = "session tick failed; the session keeps its wave and carries on.";

	public static string Suppressed(int count)
	{
		return $"({count} more like it were not logged.)";
	}

	public static string SpawnFailed(Combat.EnemyTier tier)
	{
		return $"spawning a {tier} failed; that spawn was skipped and the wave carries on.";
	}

	public const string JumpNotPossible = "laststand_wave: only works in the arena, with a wave of 1 or more.";

	public static string JumpedToWave(int wave)
	{
		return $"jumping to wave {wave}; it starts now.";
	}

	public static string WaveResumed(int wave)
	{
		return $"wave {wave} resumed after a reload; no new monsters are added to it.";
	}

	public static string ProgressRestored(int wave, bool inProgress, int adopted)
	{
		return inProgress
			? $"progress restored: wave {wave} is still being fought, {adopted} monster(s) on the field taken over."
			: $"progress restored: wave {wave} was cleared; wave {wave + 1} comes next.";
	}

	public static string WellReused(UnityEngine.Vector3 position)
	{
		return $"Dream Well already standing at {position}; reusing it.";
	}
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
		return $"heroic boss: died, dropped {chaosRewards} Corrupted Chaos reward(s).";
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
