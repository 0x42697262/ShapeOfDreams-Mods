using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Blocks every run-ending path while arena mode is on.
/// </summary>
/// <remarks>
/// <c>WrapUpAndShowResult</c> is the single funnel for concluding a run —
/// <c>CheckGameOver</c>, <c>ConcludePureWhiteDream</c> and
/// <c>ConcludeUnknownFate</c> all go through it. Blocking it here is stronger
/// than clearing <c>isGameOverEnabled</c>, which only stops the all-knocked-out
/// countdown.
///
/// This is what lets players die freely without the run wrapping up, which is
/// the behavior wanted until real win conditions exist.
/// </remarks>
[HarmonyPatch(typeof(GameManager), "WrapUpAndShowResult")]
internal static class GameEndPatch
{
	[HarmonyPrefix]
	private static bool Prefix(DewGameResult.ResultType type)
	{
		if (!DreamArenaMod.ShouldBlockRunEnd)
		{
			return true;
		}

		Debug.Log($"{ModIdentity.LogPrefix}blocked run end ({type}); arena mode is holding the game open.");
		return false;
	}
}
