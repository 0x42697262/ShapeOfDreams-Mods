using DreamArena.Game;
using DreamArena.Text;
using DreamArena.UI;
using UnityEngine;

namespace DreamArena.Config;

public class DreamArenaConfig : ModConfig
{
	[HideInInspector]
	public int schemaVersion = ConfigSchema.Current;

	[Page(SettingsPage.Match)]
	[Header(ConfigText.TeamsHeader)]
	[LabelText(ConfigText.TeamModeLabel)]
	[Description(ConfigText.TeamModeDescription)]
	public TeamMode teamMode = TeamMode.FreeForAll;

	[LabelText(ConfigText.AnnouncePhasesLabel)]
	[Description(ConfigText.AnnouncePhasesDescription)]
	public bool announcePhases = true;

	[Header(ConfigText.FlowHeader)]
	[LabelText(ConfigText.NeverEndRunLabel)]
	[Description(ConfigText.NeverEndRunDescription)]
	public bool neverEndRun = true;

	[LabelText(ConfigText.ChatRestartLabel)]
	[Description(ConfigText.ChatRestartDescription)]
	public bool chatRestart = true;

	[LabelText(ConfigText.SkipTravelVoteLabel)]
	[Description(ConfigText.SkipTravelVoteDescription)]
	public bool skipTravelVote = true;

	[Page(SettingsPage.Arena)]
	[Header(ConfigText.RoomHeader)]
	[LabelText(ConfigText.ForceArenaRoomLabel)]
	[Description(ConfigText.ForceArenaRoomDescription)]
	public bool forceArenaRoom = true;

	[PickOne(ChoiceSet.ArenaRooms)]
	[LabelText(ConfigText.ArenaRoomLabel)]
	[Description(ConfigText.ArenaRoomDescription)]
	public string arenaRoom = GameNames.DefaultArenaRoom;

	[LabelText(ConfigText.PlainArenaLabel)]
	[Description(ConfigText.PlainArenaDescription)]
	public bool plainArena = true;

	[LabelText(ConfigText.StripRoomModifiersLabel)]
	[Description(ConfigText.StripRoomModifiersDescription)]
	public bool stripRoomModifiers = true;

	[Header(ConfigText.ArrivalHeader)]
	[LabelText(ConfigText.SkipCutscenesLabel)]
	[Description(ConfigText.SkipCutscenesDescription)]
	public bool skipCutscenes = true;

	[Page(SettingsPage.Camp)]
	[Header(ConfigText.RemnantsHeader)]
	[LabelText(ConfigText.CampRemnantsLabel)]
	[Description(ConfigText.CampRemnantsDescription)]
	public bool campRemnants = true;

	[LabelText(ConfigText.RemnantMemoryLevelLabel)]
	[Description(ConfigText.RemnantMemoryLevelDescription)]
	public int remnantMemoryLevel = 10;

	[LabelText(ConfigText.RemnantEssenceQualityLabel)]
	[Description(ConfigText.RemnantEssenceQualityDescription)]
	public int remnantEssenceQuality = 400;

	[Header(ConfigText.DummyHeader)]
	[LabelText(ConfigText.TrainingDummyLabel)]
	[Description(ConfigText.TrainingDummyDescription)]
	public bool trainingDummy = true;

	[PickOne(ChoiceSet.DummyMonsters)]
	[LabelText(ConfigText.DummyMonsterLabel)]
	[Description(ConfigText.DummyMonsterDescription)]
	public string dummyMonster = GameNames.DefaultDummyMonster;

	[Page(SettingsPage.Heroes)]
	[Header(ConfigText.StrengthHeader)]
	[LabelText(ConfigText.PlayerLevelLabel)]
	[Description(ConfigText.PlayerLevelDescription)]
	public int playerLevel = 20;

	[LabelText(ConfigText.StartingMemoryLevelLabel)]
	[Description(ConfigText.StartingMemoryLevelDescription)]
	public int startingMemoryLevel = 10;

	[LabelText(ConfigText.HeroHealthMultiplierLabel)]
	[Description(ConfigText.HeroHealthMultiplierDescription)]
	public float heroHealthMultiplier = 100f;

	[Header(ConfigText.ReviveHeader)]
	[LabelText(ConfigText.AutoReviveSecondsLabel)]
	[Description(ConfigText.AutoReviveSecondsDescription)]
	public int autoReviveSeconds = 10;

	[LabelText(ConfigText.ReviveHealthPercentLabel)]
	[Description(ConfigText.ReviveHealthPercentDescription)]
	public int reviveHealthPercent = 100;

	[Page(SettingsPage.Rules)]
	[Header(ConfigText.PveHeader)]
	[LabelText(ConfigText.NoMonstersLabel)]
	[Description(ConfigText.NoMonstersDescription)]
	public bool noMonsters = true;

	[LabelText(ConfigText.RemoveGameModifiersLabel)]
	[Description(ConfigText.RemoveGameModifiersDescription)]
	public bool removeGameModifiers = true;

	[LabelText(ConfigText.RemoveLucidDreamsLabel)]
	[Description(ConfigText.RemoveLucidDreamsDescription)]
	public bool removeLucidDreams = true;

	[LabelText(ConfigText.NoRescueQuestsLabel)]
	[Description(ConfigText.NoRescueQuestsDescription)]
	public bool noRescueQuests = true;

	[LabelText(ConfigText.RemoveRunGoalLabel)]
	[Description(ConfigText.RemoveRunGoalDescription)]
	public bool removeRunGoal = true;

	public override void BuildWidgets(Transform parent, out SafeAction onChanged, out SafeAction requestUpdate)
	{
		SettingsPanel.Build(this, parent, 0f, out onChanged, out requestUpdate);
	}
}
