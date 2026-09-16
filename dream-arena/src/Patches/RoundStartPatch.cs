using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Skips the arena intro cutscene while in PvP mode.
/// </summary>
/// <remarks>
/// Walking onto a boss room's entry trigger plays a cutscene with a
/// "press space to skip" prompt before the fight. For repeated PvP rounds that
/// is just a delay, so the RPC is suppressed at the source.
///
/// <c>DewBossRoomEntry</c>'s routine still waits out the fade and performs its
/// teleport, so the flow into the arena is unchanged.
/// </remarks>
[HarmonyPatch(typeof(DewCutsceneDirector), "PlayNetworked")]
internal static class CutscenePatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled || !mod.config.skipCutscenes)
		{
			return true;
		}

		return false;
	}
}

/// <summary>
/// Spreads players around the arena as they arrive.
/// </summary>
/// <remarks>
/// <para>
/// Stock <c>TeleportAllHeroes</c> drops everyone in a tight line, which for PvP
/// means everyone lands stacked together. This runs after it
/// and repositions the heroes evenly around the arena instead.
/// </para>
/// <para>
/// Patching the teleporter rather than the caller covers both routes into the
/// arena: walking onto the entry trigger, and the <c>arena_start</c> command.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(DewHeroesTeleporter), "TeleportAllHeroes")]
internal static class ArenaSpawnPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled)
		{
			return;
		}

		ArenaSpawn.PlaceHeroes(mod.config);
	}
}
