using System.Collections.Generic;
using LastStand.Combat;
using LastStand.Config;
using LastStand.Waves;
using UnityEngine;

namespace LastStand.Text;

internal static class LogText
{
	public const string Unloaded = "unloaded.";

	public const string ModeOptionFailed = "could not add the Last Stand game mode option.";

	public const string SoloButtonFailed = "could not add the Last Stand solo button.";

	public const string LobbyButtonFailed =
		"could not add the Last Stand settings button to the lobby; use the laststand_config console command.";

	public const string ConfigSaveFailed = "could not save the Last Stand config.";

	public const string ConfigApplied = "config applied and saved.";

	public const string ConfigHostOnly =
		"laststand_config: you joined someone else's lobby, and only the host's settings apply. " +
		"The window opens on the title screen or when you host.";

	public const string HeroSoulFailed = "hero soul: could not clear a knocked-out hero's soul.";

	public const string TickFailed = "session tick failed; the session keeps its wave and carries on.";

	public const string JumpNotPossible = "laststand_wave: only works in the arena, with a wave of 1 or more.";

	public const string DreamWellName = "Dream Well";

	public const string AltarOfCleansingName = "Altar of Cleansing";

	public const string ShrineOfAscensionName = "Shrine of Ascension";

	public const string MawOfDoomName = "Maw of Doom";

	public const string ShrineOfDestinyName = "Shrine of Destiny";

	public const string HatredRemnantName = "Hatred Remnant";

	public const string ReminiscenceOfLuckName = "Reminiscence of Luck";

	public const string CheatCommandsDisabled =
		"cheat commands are off; turn on \"Allow cheat commands\" in the Last Stand settings.";

	public const string GoldNotGranted = "gold: nothing to grant.";

	public const string DreamDustNotGranted = "dream dust: nothing to grant.";

	public const string RoomClearBlocked =
		"environment: blocked the room's clear routine; the arena stays in combat until the wave ends.";

	public const string SessionStopped = "session stopped.";

	public const string NotHosting = "only the host of a Last Stand run can do that.";

	public const string GoalRemoved = "removed the run's Shape of Dreams goal; Last Stand has no destination.";

	public const string TrackerStarted = "wave tracker: started in the quest tracker.";

	public const string TrackerAdopted = "wave tracker: took over the quest left from before.";

	public const string TrackerRemoved = "wave tracker: removed from the quest tracker.";

	public const string TrackerFailed = "wave tracker: update failed.";

	public const string CheatNoHostHero = "cheat: the host has no living hero to drop it near.";

	public const string SkipNotPossible = "skip: only works while fighting in the arena.";

	public const string SettingChangeFailed = "settings: a change handler failed.";

	public const string TestBossAlreadyAlive = "test boss: one is already alive.";

	public const string TestBossNoHero = "test boss: no living host hero to spawn near.";

	public const string TestBossNotInArena = "test boss: only works while fighting in the arena.";

	public const string GuaranteedDropSource = "WaveClear";

	public static string HeroSoulRemoved(int node)
	{
		return $"hero soul: removed one from node {node}; knocked-out heroes are revived at the next wave instead.";
	}

	public const string ChangeNoteMissing =
		"workshop: no about/changenote.txt in the uploaded folder; the update goes up without a change note.";

	public static string ChangeNoteFailed(string error)
	{
		return $"workshop: could not read about/changenote.txt ({error}); the update goes up without a change note.";
	}

	public static string ChangeNoteAttached(int length)
	{
		return $"workshop: attached about/changenote.txt ({length} characters) as the update's change note.";
	}

	public const string ArenaMusicMissing =
		"boss music: this arena room has no boss music of its own; the zone's music plays.";

	public static string ArenaMusicPlayed(string effect, int connections)
	{
		return $"boss music: played the room's {effect} for the host and {connections} ready connection(s).";
	}

	public static string MidJoinOpened(MidJoinWaitType previous)
	{
		return $"mid-join: cleared the game's {previous} wait so players can join during waves.";
	}

	public static string HeroesRevived(int wave, int count)
	{
		return $"revive: {count} knocked-out hero(es) revived after wave {wave} was cleared.";
	}

	public static string MapNodesHidden(int count, int kept)
	{
		return $"world map: hid {count} node(s); only the camp and the arena (node {kept}) are shown.";
	}

	public static string Suppressed(int count)
	{
		return $"({count} more like it were not logged.)";
	}

	public static string SpawnFailed(EnemyTier tier)
	{
		return $"spawning a {tier} failed; that spawn was skipped and the wave carries on.";
	}

	public static string JumpedToWave(int wave)
	{
		return $"jumping to wave {wave}; it starts now.";
	}

	public static string WaveResumed(int wave)
	{
		return $"wave {wave} resumed after a reload; no new monsters are added to it.";
	}

	public static string ProgressRestored(int wave, bool inProgress, int adopted, int fastClears)
	{
		return (inProgress
			? $"progress restored: wave {wave} is still being fought, {adopted} monster(s) on the field taken over"
			: $"progress restored: wave {wave} was cleared; wave {wave + 1} comes next") +
		       $", {fastClears} fast clear(s) so far.";
	}

	public static string LuckChoicesRenewed(int wave)
	{
		return $"luck: picks, prays and choices renewed after wave {wave}.";
	}

	public static string HatredShardOpened(int wave, float dreamDustBase, int gold)
	{
		return $"hatred: shard opened at wave {wave}; dream dust base {dreamDustBase:0}, gold {gold}.";
	}

	public static string CurseStartsWave(int wave, string strength, int othersCursed, double seconds)
	{
		return $"curse to start: a {strength} curse was chosen, {othersCursed} other player(s) cursed; wave {wave} starts in {seconds:0.#} s.";
	}

	public static string CurseHoldStarted(int wave)
	{
		return $"curse to start: wave {wave} waits for a curse at the Hatred Remnant.";
	}

	public static string WaveStartedWithoutCurse(int wave)
	{
		return $"curse to start: wave {wave} started without a curse (no Hatred Remnant standing, or a cheat command).";
	}

	public static string HatredChoicesNeedCurse(string replaced, string with)
	{
		return $"hatred: choice {replaced} replaced with {with} so every choice is a curse.";
	}

	public static string HatredUsesReset(int wave)
	{
		return $"hatred: choices reset after wave {wave}.";
	}

	public static string CurseGoalSet(string curse, string before, int beforeAmount, int kills)
	{
		return $"curse: {curse} was {before} x{beforeAmount}; now ends after {kills} kill(s).";
	}

	public static string DestinyClaimsReset(int wave)
	{
		return $"destiny: claims reset after wave {wave}.";
	}

	public static string AscensionLocked(int firstWave)
	{
		return $"ascension: locked until wave {firstWave} is cleared.";
	}

	public static string AscensionUnlocked(int wave)
	{
		return $"ascension: unlocked after wave {wave} was cleared.";
	}

	public static string AscensionUsesReset(int wave)
	{
		return $"ascension: uses reset after wave {wave} was cleared; every player has their four uses back.";
	}

	public static string GoldPricesSet(float multiplier, int wave)
	{
		return $"gold prices: multiplier set to {multiplier:0.##} at wave {wave}.";
	}

	public static string GoldPricesRestored(float multiplier)
	{
		return $"gold prices: multiplier restored to {multiplier:0.##}.";
	}

	public static string StartingGoldGranted(int amount, int players)
	{
		return $"starting gold: granted {amount} to {players} player(s).";
	}

	public static string StartingDreamDustGranted(int amount, int players)
	{
		return $"starting dream dust: granted {amount} to {players} player(s).";
	}

	public static string GoldGranted(int amount, int players)
	{
		return $"gold: granted {amount} to {players} player(s).";
	}

	public static string FriendlyFireStarted(int wave, string strength)
	{
		return $"dark urge: wave {wave} curses every player with {strength} Dark Urge until it's cleared.";
	}

	public static string FriendlyFireEnded(int wave)
	{
		return $"dark urge: curses lifted at the end of wave {wave}; players are allies again.";
	}

	public static string EnvironmentStarted(int wave, string type)
	{
		return $"environment: wave {wave} brings {type}; the arena counts as in combat until it's cleared.";
	}

	public static string EnvironmentEnded(int wave, string type)
	{
		return $"environment: {type} removed at the end of wave {wave}.";
	}

	public static string EnvironmentFailed(string type)
	{
		return $"environment: adding {type} failed; this wave has none.";
	}

	public static string ModifierRemoveFailed(string modifier)
	{
		return $"removing {modifier} from the arena failed; it's forgotten here and goes with the room.";
	}

	public static string ShrineReused(string name, Vector3 position)
	{
		return $"{name} already standing at {position}; reusing it.";
	}

	public static string WaveSkipped(int wave, int removed)
	{
		return $"skip: wave {wave} ended early, {removed} monster(s) removed without drops.";
	}

	public static string SkipQueued(int waves, int fromWave)
	{
		return $"skip: skipping {waves} wave(s) from wave {fromWave}.";
	}

	public static string DreamDustGranted(int amount, int players)
	{
		return $"cheat: granted {amount} dream dust to {players} player(s).";
	}

	public static string SecondWindSet(int charges, float seconds, int heroes)
	{
		return charges <= 0
			? $"cheat: removed Second Wind from {heroes} hero(es)."
			: $"cheat: gave {heroes} hero(es) Second Wind x{charges} ({seconds:0.#} s invulnerable).";
	}

	public static string LevelSet(int level, int heroes)
	{
		return $"cheat: set {heroes} hero(es) to level {level} (capped at the game's max).";
	}

	public static string ItemNotFound(string kind, string query)
	{
		return $"cheat: no {kind} in this run's pool matches '{query}'. Use a rarity (common, rare, epic, legendary) or a type or display name.";
	}

	public static string ItemAmbiguous(string kind, string query, IReadOnlyList<string> candidates, int shown)
	{
		int count = candidates.Count;
		string list = string.Join(", ", System.Linq.Enumerable.Take(candidates, shown));
		return $"cheat: '{query}' matches {count} {kind}(s): {list}{(count > shown ? ", ..." : string.Empty)}. Be more specific.";
	}

	public static string ItemSpawned(string kind, string typeName, Rarity rarity, string stat, int value)
	{
		return $"cheat: dropped {kind} {typeName} ({rarity}, {stat} {value}) near the host.";
	}

	public static string WorldSet(int from, int to)
	{
		return $"starting world: zone index {from} -> {to} (label and scaling as world {to + 1}).";
	}

	public static string TrackerLabelSent(int clients)
	{
		return $"wave tracker: title sent to {clients} ready connection(s).";
	}

	public static string HeroicBossUnknown(string typeName)
	{
		return $"heroic boss: no boss named {typeName}.";
	}

	public static string HeroicBossesFromWholeList(int configured)
	{
		return configured == 0
			? "heroic boss: no types checked; picking from the whole list."
			: "heroic boss: none of the checked types can be used; picking from the whole list.";
	}

	public static string HeroicBossUnsupported(string typeName)
	{
		return $"heroic boss: {typeName} is hidden or runs its own death logic (it may end the run); skipped.";
	}

	public static string TestBossSpawned(string typeName, int level)
	{
		return $"test boss: spawned {typeName} at level {level}.";
	}

	public static string HeroicBossStats(int wave, string boss, float maxHealth, float attackDamage, float abilityPower,
		float abilityHaste)
	{
		return $"heroic boss: wave {wave} {boss} maxHealth={maxHealth:0} attackDamage={attackDamage:0} " +
		       $"abilityPower={abilityPower:0} abilityHaste={abilityHaste:0}";
	}

	public static string HeroicBossBiggestHit(int wave, string boss, float hit, string source)
	{
		return hit <= 0
			? $"heroic boss: wave {wave} {boss} died without hitting a hero."
			: $"heroic boss: wave {wave} {boss} biggest hit on a hero {hit:0} (after armor, shields included) from {source}.";
	}

	public static string HeroicBossDied(int chaosRewards, int stardust)
	{
		return $"heroic boss: died, dropped {chaosRewards} Corrupted Chaos reward(s) and {stardust} stardust.";
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

	public static string ArenaReady(int heroes, Vector3 center)
	{
		return $"arena ready: {heroes} hero(es) staged at {center}.";
	}

	public static string WaveStarted(WavePlan plan)
	{
		return $"wave {plan.Number}: regular={plan.RegularCount} surge={plan.IsSurge} hunter={plan.HunterCount} " +
		       $"merged={plan.UnmergedMobCount}/x{plan.MergeMultiplier:0.##} " +
		       $"miniboss={plan.MiniBossCount} pain={plan.PainCount} heroic={plan.HeroicBossCount} " +
		       $"heroicHaste={plan.HeroicBossHaste:0} " +
		       $"ambient={plan.AmbientLevel} " +
		       $"health=+{plan.HealthBonusPercent:0}% damage=+{plan.DamageBonusPercent:0}% " +
		       $"fastClear=x{plan.FastClearMultiplier:0.##} heroicFastClear=x{plan.HeroicFastClearMultiplier:0.##} " +
		       $"foreign={plan.ForeignTypesUnlocked} mirage={plan.MirageSkinsUnlocked} " +
		       $"mirageChance={plan.MirageChance * Tuning.PercentScale:0}% " +
		       $"huntLevel={plan.HuntLevel} cap={plan.MaxAlive}.";
	}

	public static string BossHealthRaised(int wave, string type, double before, double after, double multiple,
		double toughestMob)
	{
		return $"boss health: wave {wave} {type} maxHealth {before:0} -> {after:0} ({multiple:0.##}x the toughest monster's {toughestMob:0}).";
	}

	public static string MergeStarts(int wave, int cap)
	{
		return $"merge: from wave {wave}, waves hold at {cap} monsters and hunters and carry the rest's strength.";
	}

	public static string CatalogBuilt(int foreignTypes, int mirageSkins, int hunterStartWave, int allHuntersWave,
		int heroicBosses)
	{
		return $"catalog: {foreignTypes} monster type(s) from other worlds, {mirageSkins} Mirage skin(s), " +
		       $"{heroicBosses} heroic boss type(s); hunters start at wave {hunterStartWave}, every monster is " +
		       $"a hunter from wave {allHuntersWave}.";
	}

	public static string ModifiersStripped(int count)
	{
		return $"removed {count} room modifier(s) from arena nodes.";
	}

	public static string ModifierBlocked(string modifierType, int nodeIndex)
	{
		return $"blocked room modifier {modifierType} on arena node {nodeIndex}.";
	}

	public static string FastClearTriggered(int wave, int instantKills, int monsters, double seconds,
		double multiplier, double heroicMultiplier)
	{
		return $"fast clear: wave {wave} had {instantKills} of {monsters} monster(s) die within {seconds:0.##} s of their first hit; from wave {wave + 1} monsters have x{multiplier:0.##} health and damage, heroic bosses x{heroicMultiplier:0.##}.";
	}

	public static string FastClearMissed(int wave, int instantKills, int deaths, int needed, double seconds)
	{
		return $"fast clear: wave {wave} had {instantKills} of {deaths} death(s) within {seconds:0.##} s of the first hit; {needed} needed.";
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

	public static string ArenaPanelShown(string type)
	{
		return $"panel: {type} shown as the arena's default modifier panel.";
	}

	public static string EdgeRing(int shrines, float medianReach, float cap, int unplayableDirections)
	{
		return $"edge ring: {shrines} shrine(s), median reach {medianReach:0.0} m, capped at {cap:0.0} m; " +
		       $"{unplayableDirections} direction(s) end at unplayable ground (lava).";
	}

	public static string ShrinePlaced(string name, Vector3 position)
	{
		return $"{name} placed at {position}.";
	}

	public static string Status(bool selected, bool server, string phase, int wave, bool inProgress,
		double secondsToNext, int occupied, int pending, string items)
	{
		return $"mode={selected} server={server} phase={phase} wave={wave} inProgress={inProgress} " +
		       $"next={secondsToNext:0}s " +
		       $"alive={occupied} pending={pending} {items}.";
	}

	public static string LeftoverItemsFreed(int wave, int count)
	{
		return $"loot: wave {wave} ended with {count} reserved item(s) on the ground; freed for everyone.";
	}

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
