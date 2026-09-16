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
	[LabelText("PvP mode")]
	[Description("Normally chosen as 'PvP Arena' in the Create Lobby game mode dropdown, " +
	             "next to Shape of Dreams and Limbo. Shown here as a fallback.")]
	public bool arenaEnabled = false;

	[LabelText("Team mode")]
	[Description("Applied once players leave camp through the rift. FreeForAll: everyone " +
	             "fights everyone. TwoTeams: split by join order. Peaceful: never hostile.")]
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

	[LabelText("Remove the boss")]
	[Description("Despawns the boss room's boss so the arena stays empty. " +
	             "Despawn, not kill, so no rewards drop and no exit rift opens.")]
	public bool removeBoss = true;

	[LabelText("Hide objectives")]
	[Description("Drops the Goal and any side quests from the HUD. They mean nothing " +
	             "in an arena match.")]
	public bool hideObjectives = true;

	[LabelText("Strip room modifiers")]
	[Description("Removes modifiers like Accelerated Time from arena rooms, which would " +
	             "otherwise change everyone's movement and attack speed mid-match.")]
	public bool stripRoomModifiers = true;

	[LabelText("Skip intro cutscenes")]
	[Description("Suppresses the arena intro with its 'press space to skip' prompt, " +
	             "which is just a delay between rounds.")]
	public bool skipCutscenes = true;

	[LabelText("Spread spawns")]
	[Description("Places players evenly around the arena instead of in a tight line, " +
	             "so nobody opens the round already in attack range.")]
	public bool spreadSpawns = true;


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
		other.removeBoss = removeBoss;
		other.autoReviveSeconds = autoReviveSeconds;
		other.reviveHealthPercent = reviveHealthPercent;
		other.hideObjectives = hideObjectives;
		other.stripRoomModifiers = stripRoomModifiers;
		other.skipCutscenes = skipCutscenes;
		other.spreadSpawns = spreadSpawns;
	}
}
