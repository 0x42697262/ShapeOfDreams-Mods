using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace DreamArena.Patches;

/// <summary>
/// Runs the restart when a <c>-restart</c> vote passes, instead of the zone
/// change a next-zone vote would normally make.
/// </summary>
/// <remarks>
/// <c>PlayGameManager</c> overrides <c>LoadNextZone</c>, so a patch on the base
/// method alone would never see the call. Every <c>GameManager</c> type that
/// declares it is patched. The most-derived one runs first, consumes the
/// pending flag and skips the original, so the base never runs either.
/// </remarks>
[HarmonyPatch]
internal static class RestartVoteLoadNextZonePatch
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
		return !RestartVote.TryConsumePassed();
	}
}

/// <summary>Clears the restart flag when any vote ends, passed or canceled.</summary>
[HarmonyPatch(typeof(ZoneManager), "ClearVoteState")]
internal static class RestartVoteClearPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		RestartVote.OnVoteCleared();
	}
}

/// <summary>
/// Waives the "can't travel yet" checks for a restart vote.
/// </summary>
/// <remarks>
/// A passing vote waits for nobody to be carrying an item and for nothing to
/// be left on the floor, and cancels outright while a boss lives. The restart
/// destroys every loose and carried item itself, so waiting would only stall
/// it, with the "you left an item behind" message in everyone's chat.
/// </remarks>
[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.GetCannotTravelReason))]
internal static class RestartVoteTravelReasonPatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref (string reasonText, bool shouldCancel) __result)
	{
		if (!RestartVote.Pending)
		{
			return true;
		}

		__result = (null, false);
		return false;
	}
}
