using DreamArena.Identity;
using UnityEngine;
using UnityEngine.UI;

namespace DreamArena.UI;

internal static class InjectedUi
{
	public static void RemoveAll()
	{
		foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
		{
			if (button != null && button.gameObject.scene.IsValid() && ModIdentity.IsOurs(button.name))
			{
				Object.DestroyImmediate(button.gameObject);
			}
		}
	}
}
