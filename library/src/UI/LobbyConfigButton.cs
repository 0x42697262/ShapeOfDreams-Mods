using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SodLib.UI;

internal static class LobbyConfigButton
{
	public static void Ensure(Transform panel)
	{
		if (panel == null)
		{
			return;
		}

		bool show = ModMode.IsHosting();
		Transform existing = panel.Find(ModIdentity.LobbyButtonName);
		if (existing != null)
		{
			existing.gameObject.SetActive(show);
			return;
		}

		if (!show)
		{
			return;
		}

		Button button = Object.Instantiate(DewGUI.widgetButton, panel);
		button.name = ModIdentity.LobbyButtonName;
		button.SetHeight(ConfigWindowLayout.LobbyButtonHeight);
		button.GetComponentInChildren<TMP_Text>(includeInactive: true).text = ModIdentity.DisplayName;
		button.onClick.AddListener(ConfigWindow.Toggle);
	}
}
