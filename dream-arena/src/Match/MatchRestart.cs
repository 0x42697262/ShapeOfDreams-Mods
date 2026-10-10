using System.Collections.Generic;
using DreamArena.Arena;
using DreamArena.Diagnostics;
using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.Match;

internal static class MatchRestart
{
	private static readonly List<Hero> Heroes = new List<Hero>();

	public static string CannotRestartReason(Phase phase)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return Messages.RestartNotReady;
		}

		if (zones.isInAnyTransition)
		{
			return Messages.RestartTraveling;
		}

		if (zones.isVoting)
		{
			return Messages.RestartVoteRunning;
		}

		if (phase != Phase.Arena)
		{
			return Messages.RestartInCamp;
		}

		return ArenaLocator.CampNode(zones) == ArenaLocator.NoNode ? Messages.RestartNoCamp : null;
	}

	public static bool Execute(string requester, System.Action restock)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		int camp = zones != null ? ArenaLocator.CampNode(zones) : ArenaLocator.NoNode;
		if (camp == ArenaLocator.NoNode || zones.isInAnyTransition)
		{
			return false;
		}

		Heroes.Clear();
		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null && !player.hero.IsNullOrInactive())
			{
				Heroes.Add(player.hero);
			}
		}

		foreach (Hero hero in Heroes)
		{
			if (hero.isKnockedOut && hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
			{
				knockedOut.Revive(Config.Tuning.FullHealthRatio);
			}

			hero.Skill.StopHoldInHand();
			BuildWipe.Wipe(hero);
		}

		int swept = BuildWipe.SweepLooseItems();
		foreach (Hero hero in Heroes)
		{
			BuildWipe.RestoreLoadout(hero);
		}

		restock();
		ForgetRoomStates(zones);
		zones.TravelToNode(camp, advanceTurn: false, ignoreInterrupts: true);
		ChatNotices.Broadcast(Messages.Restarting);
		Log.Info(LogText.Restarted(requester, Heroes.Count, swept, camp));
		Heroes.Clear();
		return true;
	}

	private static void ForgetRoomStates(ZoneManager zones)
	{
		if (zones.visitedNodesSaveData == null)
		{
			return;
		}

		for (int i = 0; i < zones.visitedNodesSaveData.Count; i++)
		{
			zones.visitedNodesSaveData[i] = null;
		}
	}
}
