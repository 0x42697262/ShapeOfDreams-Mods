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
	[Description("Sets the monster population budget to zero so the arena stays empty. " +
	             "This is also what keeps the boss from showing up.")]
	public bool suppressMonsters = true;

	[LabelText("Force arena room")]
	[Description("Redirects every room you travel to into the arena room below.")]
	public bool forceArenaRoom = true;

	[LabelText("Arena room")]
	[Description("Room asset name. Default is the Lone King's Zenith, the Primus boss arena. " +
	             "Others: Room_Ink_Boss_0, Room_Despair_Boss_0, Room_Sky_Boss_0, " +
	             "Room_Forest_Boss_0, Room_LavaLand_Boss_0, Room_DarkCave_Boss_0, " +
	             "Room_SnowMountain_Boss_0.")]
	public string arenaRoom = "Room_Primus_Boss_0";

	[LabelText("Auto revive seconds")]
	[Description("Seconds a knocked-out player waits before being revived. 0 disables it.")]
	public int autoReviveSeconds = 10;

	[LabelText("Revive health percent")]
	[Description("Health to revive with, as a percentage of maximum.")]
	public int reviveHealthPercent = 100;

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
		other.forceArenaRoom = forceArenaRoom;
		other.arenaRoom = arenaRoom;
		other.autoReviveSeconds = autoReviveSeconds;
		other.reviveHealthPercent = reviveHealthPercent;
	}
}
