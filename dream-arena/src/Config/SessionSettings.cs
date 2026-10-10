using System;
using DreamArena.Game;
using UnityEngine;

namespace DreamArena.Config;

internal sealed class SessionSettings
{
	public TeamMode TeamMode { get; private set; }

	public bool AnnouncePhases { get; private set; }

	public bool NeverEndRun { get; private set; }

	public bool ChatRestart { get; private set; }

	public bool SkipTravelVote { get; private set; }

	public bool ForceArenaRoom { get; private set; }

	public string ArenaRoom { get; private set; }

	public bool PlainArena { get; private set; }

	public bool StripRoomModifiers { get; private set; }

	public bool SkipCutscenes { get; private set; }

	public bool CampRemnants { get; private set; }

	public int RemnantMemoryLevel { get; private set; }

	public int RemnantEssenceQuality { get; private set; }

	public bool TrainingDummy { get; private set; }

	public string DummyMonster { get; private set; }

	public bool MaxPlayerLevel { get; private set; }

	public int StartingMemoryLevel { get; private set; }

	public float HeroHealthMultiplier { get; private set; }

	public float AutoReviveSeconds { get; private set; }

	public float ReviveHealthRatio { get; private set; }

	public bool NoMonsters { get; private set; }

	public bool RemoveGameModifiers { get; private set; }

	public bool RemoveLucidDreams { get; private set; }

	public bool NoRescueQuests { get; private set; }

	public bool RemoveRunGoal { get; private set; }

	public static SessionSettings From(DreamArenaConfig config)
	{
		return new SessionSettings
		{
			TeamMode = Enum.IsDefined(typeof(TeamMode), config.teamMode) ? config.teamMode : TeamMode.FreeForAll,
			AnnouncePhases = config.announcePhases,
			NeverEndRun = config.neverEndRun,
			ChatRestart = config.chatRestart,
			SkipTravelVote = config.skipTravelVote,
			ForceArenaRoom = config.forceArenaRoom,
			ArenaRoom = string.IsNullOrWhiteSpace(config.arenaRoom) ? GameNames.DefaultArenaRoom : config.arenaRoom.Trim(),
			PlainArena = config.plainArena,
			StripRoomModifiers = config.stripRoomModifiers,
			SkipCutscenes = config.skipCutscenes,
			CampRemnants = config.campRemnants,
			RemnantMemoryLevel = Math.Max(Tuning.MinOfferLevel, config.remnantMemoryLevel),
			RemnantEssenceQuality = Math.Max(Tuning.MinOfferLevel, config.remnantEssenceQuality),
			TrainingDummy = config.trainingDummy,
			DummyMonster = string.IsNullOrWhiteSpace(config.dummyMonster)
				? GameNames.DefaultDummyMonster
				: config.dummyMonster.Trim(),
			MaxPlayerLevel = config.maxPlayerLevel,
			StartingMemoryLevel = Math.Max(Tuning.MemoryFloorOff, config.startingMemoryLevel),
			HeroHealthMultiplier = Mathf.Max(Tuning.NormalHealthMultiplier, config.heroHealthMultiplier),
			AutoReviveSeconds = Math.Max(0, config.autoReviveSeconds),
			ReviveHealthRatio = Mathf.Clamp(config.reviveHealthPercent, Tuning.MinRevivePercent,
				Tuning.MaxRevivePercent) / Tuning.PercentScale,
			NoMonsters = config.noMonsters,
			RemoveGameModifiers = config.removeGameModifiers,
			RemoveLucidDreams = config.removeLucidDreams,
			NoRescueQuests = config.noRescueQuests,
			RemoveRunGoal = config.removeRunGoal,
		};
	}
}
