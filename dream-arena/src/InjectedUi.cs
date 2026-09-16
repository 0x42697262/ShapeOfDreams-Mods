using System;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Removes everything this mod put in the scene, so live reload is clean.
/// </summary>
internal static class InjectedUi
{
	public static void CleanupAll()
	{
		ArenaConfigWindow.Close();

		foreach (UI_Window window in Resources.FindObjectsOfTypeAll<UI_Window>())
		{
			if (IsRuntimeSceneObject(window) && ModIdentity.IsOurs(window.name))
			{
				UnityEngine.Object.DestroyImmediate(window.gameObject);
			}
		}

		foreach (UI_Lobby_SettingsWindow panel in Resources.FindObjectsOfTypeAll<UI_Lobby_SettingsWindow>())
		{
			if (!IsRuntimeSceneObject(panel))
			{
				continue;
			}

			for (int i = panel.transform.childCount - 1; i >= 0; i--)
			{
				Transform child = panel.transform.GetChild(i);
				if (ModIdentity.IsOurs(child.name))
				{
					UnityEngine.Object.DestroyImmediate(child.gameObject);
				}
			}
		}
	}

	/// <summary>
	/// True for objects in a loaded scene, as opposed to the prefabs and assets
	/// that <c>FindObjectsOfTypeAll</c> also returns.
	/// </summary>
	private static bool IsRuntimeSceneObject(Component component)
	{
		return component != null
		       && component.gameObject.scene.IsValid()
		       && component.gameObject.scene.isLoaded;
	}
}
