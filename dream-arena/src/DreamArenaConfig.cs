namespace DreamArena;

/// <summary>
/// Host-side settings for an arena session.
/// </summary>
/// <remarks>
/// Doubles as the persisted mod config (the game loads and saves it via the
/// mod manager) and as the live session config shown in the in-lobby window,
/// which builds its widgets from this same type through
/// <c>DewGUI.CreateWidgetsForObject</c>.
///
/// Only the host ever sees or needs this. Every setting is enforced
/// server-side, so clients need no mod.
/// </remarks>
public class DreamArenaConfig : ModConfig
{
	[LabelText("Arena mode")]
	[Description("Master switch. When off, this mod does nothing and the run plays normally.")]
	public bool arenaEnabled = false;

	[LabelText("Team mode")]
	[Description("FreeForAll: everyone fights everyone. TwoTeams: split by join order. " +
	             "Peaceful: no hostility, for setting up between rounds.")]
	public TeamMode teamMode = TeamMode.FreeForAll;

	[LabelText("Never end the run")]
	[Description("Blocks every run-ending path, so the game continues even when every " +
	             "player is knocked out. Leave on until win conditions exist.")]
	public bool neverEndRun = true;

	[LabelText("No monsters")]
	[Description("Sets the monster population budget to zero so the arena stays empty.")]
	public bool suppressMonsters = true;

	[LabelText("Pure white dream")]
	[Description("Applies the RoomMod_PureDream room modifier for the white arena look. " +
	             "Cosmetic; no boss is spawned by this.")]
	public bool pureWhiteDream = true;

	/// <summary>Copies the live values onto another instance without replacing the object.</summary>
	public void CopyValuesTo(DreamArenaConfig other)
	{
		if (other == null)
		{
			return;
		}

		other.arenaEnabled = arenaEnabled;
		other.teamMode = teamMode;
		other.neverEndRun = neverEndRun;
		other.suppressMonsters = suppressMonsters;
		other.pureWhiteDream = pureWhiteDream;
	}
}
