using DreamArena.Config;
using UnityEngine;

namespace DreamArena.Text;

internal static class LogText
{
	public const string ConfigHostOnly =
		"arena: you joined someone else's lobby, and only the host's settings apply. The window opens on " +
		"the title screen or when you host.";

	public const string NotHosting = "only the host of a PvP Arena run can do that.";

	public const string RulesRestored = "stock game rules restored.";

	public const string SessionStopped = "PvP Arena session ended; relations and rules restored.";

	public const string RemnantPrefabsMissing = "camp remnants: the memory or essence remnant prefab was not found.";

	public const string RestartVoteFailed = "restart vote passed, but the restart could not run.";

	public const string ScanNoHero = "scan: no living hero to measure relations from.";

	public const string ScanNoAnchor = "scan: no arena anchor in this room.";

	public static string SessionStarted(TeamMode mode)
	{
		return $"PvP Arena session started ({mode} once players leave camp).";
	}

	public static string RulesApplied(bool gameOverEnabled, float populationMultiplier, int lucidDreamsCleared)
	{
		return $"rules applied: gameOverEnabled={gameOverEnabled}, populationMultiplier={populationMultiplier}, " +
		       $"lucid dreams cleared={lucidDreamsCleared}.";
	}

	public static string PhaseChanged(ArenaPhase phase, TeamMode mode)
	{
		return $"phase: {phase} ({mode}).";
	}

	public static string RelationsApplied(TeamMode mode, int players, int hostilePairs)
	{
		return $"relations: {mode} for {players} player(s), {hostilePairs} hostile pair(s).";
	}

	public static string HeroesStaged(int heroes, string source, Vector3 center, float ring)
	{
		return $"staging: placed {heroes} hero(es) around {source} at {center}, ring {ring:0.#} m.";
	}

	public static string PveSwept(int count)
	{
		return $"pve: removed {count} monster(s), prop(s), shrine(s) or loose item(s) from the arena.";
	}

	public static string RemnantStock(string remnant, int items)
	{
		return $"camp remnants: {remnant} offers {items} item(s).";
	}

	public static string RemnantsBuilt(int count, Vector3 center)
	{
		return $"camp remnants: placed {count} around the rift at {center}.";
	}

	public static string RemnantRingBlocked(int count)
	{
		return $"camp remnants: no room for a ring of {count} around the rift.";
	}

	public static string RemnantsGaveUp(int node, int attempts)
	{
		return $"camp remnants: gave up on node {node} after {attempts} attempt(s).";
	}

	public static string DummyMonsterMissing(string monster)
	{
		return $"training dummy: monster '{monster}' not found; no dummy.";
	}

	public static string DummyPlaced(string monster, Vector3 home)
	{
		return $"training dummy: {monster} placed at {home}.";
	}

	public static string HeroLevelled(string hero, int level)
	{
		return $"heroes: {hero} raised to level {level}.";
	}

	public static string MemoryRaised(string hero, HeroSkillLocation slot, string memory, int from, int to)
	{
		return $"heroes: {hero}'s {slot} memory {memory} raised from level {from} to {to}.";
	}

	public static string MemoryLocked(string hero, HeroSkillLocation slot, string memory, int level)
	{
		return $"heroes: {hero}'s {slot} memory {memory} stays at level {level}; the game marks it not upgradable.";
	}

	public static string HealthBoosted(string hero, float multiplier)
	{
		return $"heroes: {hero} has x{multiplier:0.##} max health.";
	}

	public static string RescueQuestSuppressed(string hero)
	{
		return $"revive: no rescue quest for {hero}.";
	}

	public static string Revived(int heroes)
	{
		return $"revive: {heroes} knocked-out hero(es) back up at full health.";
	}

	public static string HeroRevived(string hero, float healthRatio)
	{
		return $"revive: {hero} back up at {healthRatio:P0} health.";
	}

	public static string RestartVoteStarted(string requester)
	{
		return $"restart: vote started by {requester}.";
	}

	public static string Restarted(string requester, int heroes, int swept, int camp)
	{
		return $"restart: by {requester}; wiped {heroes} build(s), removed {swept} loose item(s), traveling to " +
		       $"camp node {camp}.";
	}

	public static string Status(bool selected, bool active, ArenaPhase phase, TeamMode mode, int players,
		bool gameOverEnabled, bool votePending)
	{
		return $"status: mode={selected} active={active} phase={phase} teams={mode} players={players} " +
		       $"gameOverEnabled={gameOverEnabled} restartVote={votePending}";
	}

	public static string ScanAnchor(string source, Vector3 center, float radius)
	{
		return $"scan: arena anchor {source} at {center}, radius {radius:0.#} m.";
	}

	public static string ScanEntity(bool attackable, string type, string name, string owner, EntityRelation relation,
		bool active)
	{
		return $"scan: {(attackable ? "ATTACKABLE" : "ignored   ")} {type} '{name}' owner={owner ?? "none"} " +
		       $"relation={relation} active={active}";
	}

	public static string ScanActorCount(string type, int count)
	{
		return $"scan: x{count} {type}";
	}

	public static string ScanSummary(string hero, int attackable, int entities, int actors)
	{
		return $"scan: from {hero}, {attackable} of {entities} entities attackable; {actors} actors in total.";
	}
}
