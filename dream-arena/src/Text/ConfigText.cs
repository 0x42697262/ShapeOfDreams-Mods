using DreamArena.Config;

namespace DreamArena.Text;

internal static class ConfigText
{
	public const string WindowTitle = "Dream Arena";

	public const string WindowNote = "Settings apply to this lobby. Only the host needs the mod.";

	public const string LobbyButton = "Dream Arena";

	public const string ApplyButton = "Apply";

	public const string CancelButton = "Cancel";

	public const string TeamsHeader = "Teams";

	public const string FlowHeader = "Match flow";

	public const string RoomHeader = "Room";

	public const string ArrivalHeader = "Arrival";

	public const string RemnantsHeader = "Remnants";

	public const string DummyHeader = "Training dummy";

	public const string StrengthHeader = "Strength";

	public const string ReviveHeader = "Revive";

	public const string PveHeader = "PvE";

	public const string TeamModeLabel = "Team mode";

	public const string TeamModeDescription =
		"Applies once players leave camp. Free For All: everyone fights everyone. Two Teams: split by join " +
		"order. Peaceful: nobody can be hurt.";

	public const string AnnouncePhasesLabel = "Announce in chat";

	public const string AnnouncePhasesDescription =
		"Says in chat when the arena goes live and when everyone is back in camp, so every player knows " +
		"the rules in force.";

	public const string NeverEndRunLabel = "Never end the run";

	public const string NeverEndRunDescription =
		"The run goes on even when every player is knocked out.";

	public const string ChatRestartLabel = "Chat -restart";

	public const string ChatRestartDescription =
		"Any player can type -restart during a fight to call a vote. If it passes, everyone goes back to " +
		"camp with builds wiped and remnants restocked.";

	public const string SkipTravelVoteLabel = "Skip travel vote";

	public const string SkipTravelVoteDescription =
		"Travels as soon as someone confirms the rift, without the world map vote.";

	public const string ForceArenaRoomLabel = "Arena for every room";

	public const string ForceArenaRoomDescription =
		"Every room past the camp loads as the arena room below.";

	public const string ArenaRoomLabel = "Arena room";

	public const string ArenaRoomDescription = "Boss room the fights happen in.";

	public const string PlainArenaLabel = "Plain arena";

	public const string PlainArenaDescription =
		"Loads the arena already cleared: lit, barriers open, no entry trigger. Floor hazards such as " +
		"lava stay on.";

	public const string StripRoomModifiersLabel = "No room modifiers";

	public const string StripRoomModifiersDescription =
		"Removes modifiers such as Accelerated Time from arena rooms, so a match stays even.";

	public const string SkipCutscenesLabel = "Skip intro cutscenes";

	public const string SkipCutscenesDescription = "No boss intro and no skip prompt between rounds.";

	public const string CampRemnantsLabel = "Camp remnants";

	public const string CampRemnantsDescription =
		"Ten free remnants ring the camp rift, one memory and one essence remnant per rarity. Each " +
		"player can take each item once per round.";

	public const string RemnantMemoryLevelLabel = "Memory level";

	public const string RemnantMemoryLevelDescription = "Level every memory is offered at.";

	public const string RemnantEssenceQualityLabel = "Essence quality";

	public const string RemnantEssenceQualityDescription =
		"Quality every essence is offered at. 200 Regular, 400 Flawless, 600 Perfect, 1000 Otherworldly.";

	public const string TrainingDummyLabel = "Training dummy";

	public const string TrainingDummyDescription =
		"A monster beside the remnants that never moves, fights back or dies, for trying out builds.";

	public const string DummyMonsterLabel = "Dummy monster";

	public const string DummyMonsterDescription = "Monster used as the dummy.";

	public const string PlayerLevelLabel = "Player level";

	public const string PlayerLevelDescription =
		"Every hero is raised to this level, up to their level cap (20, more with Chaos level-cap bonuses). " +
		"Heroes above it keep their level. 0 = leave levels alone.";

	public const string StartingMemoryLevelLabel = "Starting memory level";

	public const string StartingMemoryLevelDescription =
		"Raises every equipped memory, identity included, to at least this level. 1 turns it off.";

	public const string HeroHealthMultiplierLabel = "Hero health multiplier";

	public const string HeroHealthMultiplierDescription =
		"Multiplies every hero's maximum health, for longer fights. 1 is normal.";

	public const string AutoReviveSecondsLabel = "Revive after (seconds)";

	public const string AutoReviveSecondsDescription =
		"Seconds a knocked-out player waits before getting back up. 0 turns it off.";

	public const string ReviveHealthPercentLabel = "Revive health (%)";

	public const string ReviveHealthPercentDescription = "Health a revived player gets back.";

	public const string NoMonstersLabel = "No monsters";

	public const string NoMonstersDescription =
		"No monsters, boss, props, chests or loose items in the arena.";

	public const string RemoveGameModifiersLabel = "No game modifiers";

	public const string RemoveGameModifiersDescription =
		"Removes Obliviax, Mirage Skin, Golden Lizard and the other run-wide PvE modifiers.";

	public const string RemoveLucidDreamsLabel = "No lucid dreams";

	public const string RemoveLucidDreamsDescription = "Lucid dreams can't be picked and none apply.";

	public const string NoRescueQuestsLabel = "No rescue quests";

	public const string NoRescueQuestsDescription =
		"A knock-out starts no Lost Soul quest and leaves no soul on the map.";

	public const string RemoveRunGoalLabel = "No run goal";

	public const string RemoveRunGoalDescription =
		"Removes the Shape of Dreams goal from the quest tracker, for every player.";

	public const string PrimusWorld = "Primus";

	public const string ForestWorld = "Forest";

	public const string DarkCaveWorld = "Dark Cave";

	public const string DespairWorld = "Despair";

	public const string SkyWorld = "Sky";

	public const string SnowMountainWorld = "Snow Mountain";

	public const string LavaLandWorld = "Lava Land";

	public const string InkWorld = "Ink";

	public static string ChoiceLabel(string name, string detail)
	{
		return name == null ? detail : $"{name} ({detail})";
	}

	public static string UnknownChoiceLabel(string value)
	{
		return $"{value} (not in the list)";
	}

	public static string PageTitle(SettingsPage page)
	{
		switch (page)
		{
			case SettingsPage.Arena:
				return "Arena";
			case SettingsPage.Camp:
				return "Camp";
			case SettingsPage.Heroes:
				return "Heroes";
			case SettingsPage.Rules:
				return "Rules";
			default:
				return "Match";
		}
	}

	public static string PageSubtitle(SettingsPage page)
	{
		switch (page)
		{
			case SettingsPage.Arena:
				return "Where the fights happen and how players arrive.";
			case SettingsPage.Camp:
				return "Free remnants and a training dummy between rounds.";
			case SettingsPage.Heroes:
				return "Levels, health and revives.";
			case SettingsPage.Rules:
				return "What PvE content is kept out.";
			default:
				return "Teams, the run's end and restarts.";
		}
	}
}
