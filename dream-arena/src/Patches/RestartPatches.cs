using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using DreamArena.Game;
using DreamArena.Mode;
using DreamArena.Text;
using Mirror;

namespace DreamArena.Patches;

[HarmonyPatch(typeof(ChatManager), GameNames.ChatCommand)]
internal static class ChatRestartPatch
{
	[HarmonyPostfix]
	private static void Postfix(string content, NetworkConnectionToClient sender)
	{
		if (!ArenaMode.IsHosting() || content == null || sender == null
		    || !string.Equals(content.Trim(), GameNames.RestartChatCommand, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		DewPlayer player = sender.GetPlayer();
		string reason = DreamArenaMod.Current?.Session?.RequestRestart(player);
		if (reason != null)
		{
			ChatNotices.Tell(player, Messages.CannotRestart(reason));
		}
	}
}

[HarmonyPatch]
internal static class RestartVotePassedPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(GameManager).Assembly))
		{
			if (!typeof(GameManager).IsAssignableFrom(type))
			{
				continue;
			}

			MethodInfo method = AccessTools.DeclaredMethod(type, nameof(GameManager.LoadNextZone), Type.EmptyTypes);
			if (method != null && !method.IsAbstract)
			{
				yield return method;
			}
		}
	}

	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !(NetworkServer.active && DreamArenaMod.Current?.Session?.TryConsumeRestartVote() == true);
	}
}

[HarmonyPatch(typeof(ZoneManager), GameNames.ClearVoteState)]
internal static class RestartVoteClearPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		DreamArenaMod.Current?.Session?.OnVoteCleared();
	}
}

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.GetCannotTravelReason))]
internal static class RestartVoteTravelReasonPatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref (string reasonText, bool shouldCancel) __result)
	{
		if (DreamArenaMod.Current?.Session?.RestartVotePending != true)
		{
			return true;
		}

		__result = (null, false);
		return false;
	}
}
