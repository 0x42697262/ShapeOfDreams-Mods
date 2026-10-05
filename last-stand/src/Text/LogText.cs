using LastStand.Combat;
using LastStand.Waves;

namespace LastStand.Text;

internal static class LogText
{
	public const string Unloaded = "unloaded.";
	public const string ModeOptionFailed = "could not add the Last Stand game mode option.";
	public const string SoloButtonFailed = "could not add the Last Stand solo button.";
	public const string TickFailed = "session tick failed; the session was stopped.";
	public const string SessionStopped = "session stopped.";
	public const string NotHosting = "only the host of a Last Stand run can do that.";

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
		return $"wave {plan.Number}: regular={plan.RegularCount} hunter={plan.HunterCount} " +
		       $"miniboss={plan.MiniBossCount} pain={plan.PainCount} ambient={plan.AmbientLevel} " +
		       $"health=+{plan.HealthBonusPercent:0}% damage=+{plan.DamageBonusPercent:0}% " +
		       $"foreign={plan.ForeignTypesUnlocked} mirage={plan.MirageSkinsUnlocked} " +
		       $"huntLevel={plan.HuntLevel} cap={plan.MaxAlive}.";
	}

	public static string CatalogBuilt(int foreignTypes, int mirageSkins, int hunterStartWave)
	{
		return $"catalog: {foreignTypes} monster type(s) from other worlds, {mirageSkins} Mirage skin(s); " +
		       $"hunters start at wave {hunterStartWave}.";
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
		double secondsToNext, int occupied, int pending, int loot)
	{
		return $"mode={selected} server={server} phase={phase} wave={wave} inProgress={inProgress} " +
		       $"next={secondsToNext:0}s " +
		       $"alive={occupied} pending={pending} loot={loot}.";
	}
}
