using HarmonyLib;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Adds PvP to the lobby's difficulty row, next to Normal, Nightmare and Limbo.
/// </summary>
/// <remarks>
/// <para>
/// PvP belongs where you already choose how to play, not behind a checkbox in a
/// settings window. The row is built by <c>UI_Lobby_DifficultyGroup.Start</c>,
/// which instantiates one <c>UI_Lobby_DifficultyItem</c> per entry in its
/// <c>difficulties</c> list; this adds one more from the same prefab.
/// </para>
/// <para>
/// The extra item borrows a real difficulty's visuals. <c>UI_DifficultyIcon</c>
/// resolves its sprite with <c>DewResources.GetByName&lt;DewDifficultySettings&gt;(type)</c>,
/// so an invented type would fail to load; it is tinted instead so it reads as
/// its own option.
/// </para>
/// <para>
/// Selection stays mutually exclusive: clicking PvP turns arena mode on, and
/// clicking any real difficulty turns it off. The real
/// <c>GameSettingsManager.difficulty</c> is never given an unknown value.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(UI_Lobby_DifficultyGroup))]
internal static class GameModeGroupPatch
{
	/// <summary>Existing difficulty whose icon the PvP option borrows.</summary>
	private const string BorrowedIcon = "diffNightmare";

	private static readonly Color PvpTint = new Color(1f, 0.35f, 0.35f);

	[HarmonyPostfix]
	[HarmonyPatch("Start")]
	private static void Start_Postfix(UI_Lobby_DifficultyGroup __instance)
	{
		if (__instance == null || __instance.parent == null || __instance.prefab == null)
		{
			return;
		}

		// Idempotent: the row is rebuilt when a Limbo game is announced.
		foreach (Transform child in __instance.parent)
		{
			if (ModIdentity.IsOurs(child.name))
			{
				return;
			}
		}

		try
		{
			UI_Lobby_DifficultyItem item = Object.Instantiate(__instance.prefab, __instance.parent);
			item.name = "Difficulty Item - PvP" + ModIdentity.OwnedSuffix;

			if (item.icon != null)
			{
				item.icon.Setup(BorrowedIcon);

				if (item.icon.icon != null)
				{
					item.icon.icon.color = PvpTint;
				}

				if (item.icon.outline != null)
				{
					item.icon.outline.effectColor = PvpTint * 0.4f;
				}
			}

			GameModeItemPatch.Refresh(item);
			Debug.Log($"{ModIdentity.LogPrefix}PvP option added to the difficulty row.");
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}could not add the PvP option " +
			                 $"(use the Dream Arena window or 'arena_on'): {e.Message}");
		}
	}
}

/// <summary>
/// Makes the PvP item behave like a mode choice rather than a difficulty.
/// </summary>
[HarmonyPatch(typeof(UI_Lobby_DifficultyItem))]
internal static class GameModeItemPatch
{
	private static bool IsPvpItem(UI_Lobby_DifficultyItem item)
	{
		return item != null && ModIdentity.IsOurs(item.name);
	}

	/// <summary>
	/// Intercepts clicks: PvP toggles arena mode, anything else leaves it.
	/// </summary>
	[HarmonyPrefix]
	[HarmonyPatch("Click")]
	private static bool Click_Prefix(UI_Lobby_DifficultyItem __instance)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null)
		{
			return true;
		}

		if (IsPvpItem(__instance))
		{
			mod.config.arenaEnabled = true;
			mod.ApplyAndPersist();
			RefreshAll();

			// Skip the original: it would write an unknown difficulty.
			return false;
		}

		// A real difficulty was chosen, so leave PvP mode.
		if (mod.config.arenaEnabled)
		{
			mod.config.arenaEnabled = false;
			mod.ApplyAndPersist();
			ArenaRules.Restore();
			RefreshAll();
		}

		return true;
	}

	/// <summary>
	/// Drives the PvP item's selected highlight from arena mode instead of from
	/// the difficulty string, which never matches it.
	/// </summary>
	[HarmonyPostfix]
	[HarmonyPatch("OnDifficultyChanged")]
	private static void OnDifficultyChanged_Postfix(UI_Lobby_DifficultyItem __instance)
	{
		if (IsPvpItem(__instance))
		{
			Refresh(__instance);
		}
	}

	public static void Refresh(UI_Lobby_DifficultyItem item)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (item == null || mod == null || item.isSelectedObject == null)
		{
			return;
		}

		bool selected = mod.config.arenaEnabled;
		item.isSelectedObject.SetActive(selected);

		CanvasGroup group = item.icon != null ? item.icon.GetComponent<CanvasGroup>() : null;
		if (group != null)
		{
			// Matches the stock convention: the chosen icon is the dimmed one.
			group.alpha = selected ? 0.2f : 1f;
		}
	}

	/// <summary>Refreshes every difficulty item after a mode change.</summary>
	public static void RefreshAll()
	{
		foreach (UI_Lobby_DifficultyItem item in
		         Resources.FindObjectsOfTypeAll<UI_Lobby_DifficultyItem>())
		{
			if (item == null || !item.gameObject.scene.isLoaded)
			{
				continue;
			}

			if (IsPvpItem(item))
			{
				Refresh(item);
			}
		}
	}
}
