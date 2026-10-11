namespace DreamArena.Text;

internal static class CommandText
{
	public const string Config = "arena";
	public const string ConfigDescription = "Open the Dream Arena settings window (host or title screen)";

	public const string Status = "arena_status";
	public const string StatusDescription = "Report the Dream Arena session state";

	public const string Start = "arena_start";
	public const string StartDescription = "Clear the arena of PvE and place every player in the ring again (host)";

	public const string Restart = "arena_restart";
	public const string RestartDescription = "Restart at once without a vote: back to camp, builds wiped (host)";

	public const string Revive = "arena_revive";
	public const string ReviveDescription = "Revive every knocked-out player at full health now (host)";

	public const string Scan = "arena_scan";
	public const string ScanDescription = "List everything in this room a hero could attack";

	public const string Purge = "arena_purge";
	public const string PurgeDescription = "Remove every monster, prop and loose item from this room (host)";
}
