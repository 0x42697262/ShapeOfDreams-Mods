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

	[LabelText("Remove the boss")]
	[Description("Despawns the boss room's boss so the arena stays empty. " +
	             "Despawn, not kill, so no rewards drop and no exit rift opens.")]
	public bool removeBoss = true;

	[LabelText("Auto revive seconds")]
	[Description("Seconds a knocked-out player waits before being revived. 0 disables it.")]
	public int autoReviveSeconds = 10;

	[LabelText("Revive health percent")]
	[Description("Health to revive with, as a percentage of maximum.")]
	public int reviveHealthPercent = 100;

	[LabelText("Loadouts from deja vu")]
	[Description("Banks every deja vu item a player picks in the lobby and grants the whole " +
	             "set at run start. Players pick one at a time in the stock UI; the host " +
	             "remembers the sequence. Clearing the slot starts their list over.")]
	public bool loadoutEnabled = true;

	[LabelText("Max essences")]
	[Description("Most essences one player can bank. A full build is 12.")]
	public int maxEssences = 12;

	[LabelText("Max memories")]
	[Description("Most memories one player can bank. A full build is 2.")]
	public int maxMemories = 2;

	[LabelText("Essence quality")]
	[Description("Fixed quality every granted essence gets, so builds compete on composition " +
	             "rather than luck. Tiers: 50 Chipped, 100 Flawed, 200 Regular, 400 Flawless, " +
	             "600 Perfect, 1000 Otherworldly, above that Transcendent. Stock deja vu gives 100.")]
	public int essenceQuality = 400;

	[LabelText("Memory level")]
	[Description("Fixed level every granted memory gets. Stock deja vu gives level 1.")]
	public int memoryLevel = 1;

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
		other.loadoutEnabled = loadoutEnabled;
		other.maxEssences = maxEssences;
		other.maxMemories = maxMemories;
		other.essenceQuality = essenceQuality;
		other.memoryLevel = memoryLevel;
	}
}
