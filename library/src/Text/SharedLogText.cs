namespace SodLib.Text;

internal static class SharedLogText
{
	public const string Unloaded = "unloaded.";

	public const string ConfigApplied = "config applied and saved.";

	public const string ConfigSaveFailed = "could not save the " + ModIdentity.DisplayName + " config.";

	public const string ModeOptionFailed = "could not add the " + ModIdentity.DisplayName + " game mode option.";

	public const string LobbyButtonFailed =
		"could not add the " + ModIdentity.DisplayName + " settings button to the lobby; use the console's config command.";

	public const string GoalRemoved =
		"removed the run's Shape of Dreams goal; " + ModIdentity.DisplayName + " has no destination.";

	public const string SettingChangeFailed = "settings: a change handler failed.";

	public const string TravelVotesOff =
		"travel votes: off on the host for this run (ZoneManager.forceVoteForDebug = false).";

	public const string TravelVoteCheckFailed = "travel vote: the mod's skip check failed; the game's vote rule applies.";

	public const string CutsceneCheckFailed = "cutscenes: the mod's skip check failed; the cutscene plays.";

	public const string WorldGenFailed = "world gen: the mod's handler failed; the world is left as generated.";

	public const string ChangeNoteMissing =
		"workshop: no about/changenote.txt in the uploaded folder; the update goes up without a change note.";

	public static string ArenaRoomApplied(string room, int nodes, int modifiers)
	{
		return $"arena room '{room}' applied to {nodes} node(s); {modifiers} room modifier(s) removed.";
	}

	public static string ArenaRoomFailed(string room)
	{
		return $"could not apply arena room '{room}'.";
	}

	public static string PlainArenaPrepared(string room, int triggers)
	{
		return $"plain arena: '{room}' loads cleared; {triggers} entry trigger(s) disabled.";
	}

	public static string MapNodesHidden(int count, int kept)
	{
		return $"world map: hid {count} node(s); only the camp and the arena (node {kept}) are shown.";
	}

	public static string VersionAnnounced(string version)
	{
		return $"version: announced v{version} in chat.";
	}

	public static string ArenaRoomPicked(string room, int choices)
	{
		return $"arena room: picked '{room}' at random from {choices} room(s) for this run.";
	}

	public static string ArenaRoomRestored(string room)
	{
		return $"arena room: an arena node had lost '{room}' before the camp rift opened; applied it again.";
	}

	public static string CampRiftDirected(int node)
	{
		return $"camp rift now leads straight to node {node}.";
	}

	public static string Loaded(string name, string version)
	{
		return $"loaded: {name} v{version}.";
	}

	public static string ChangeNoteFailed(string error)
	{
		return $"workshop: could not read about/changenote.txt ({error}); the update goes up without a change note.";
	}

	public static string ChangeNoteAttached(int length)
	{
		return $"workshop: attached about/changenote.txt ({length} characters) as the update's change note.";
	}

	public static string WithSuppressed(string message, int count)
	{
		return count > 0 ? $"{message} ({count} more like it were not logged.)" : message;
	}

	public static string SettingChanged(string line)
	{
		return $"settings: {line}";
	}

	public static string ConfigReset(int schemaVersion, string backupPath)
	{
		return $"saved config is from an older version; reset to version {schemaVersion} defaults. " +
		       $"Old file kept at {backupPath}.";
	}
}
