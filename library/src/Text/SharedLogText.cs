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

	public const string ChangeNoteMissing =
		"workshop: no about/changenote.txt in the uploaded folder; the update goes up without a change note.";

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
