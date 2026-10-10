using DreamArena.Identity;
using DreamArena.Text;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamArena.UI;

internal static class LobbyConfigButton
{
	public static void Ensure(Transform panel)
	{
		if (panel == null)
		{
			return;
		}

		Transform existing = panel.Find(ModIdentity.LobbyButtonName);
		if (existing != null)
		{
			existing.gameObject.SetActive(NetworkServer.active);
			return;
		}

		if (!NetworkServer.active)
		{
			return;
		}

		Button button = Object.Instantiate(DewGUI.widgetButton, panel);
		button.name = ModIdentity.LobbyButtonName;
		button.SetHeight(ConfigWindowLayout.LobbyButtonHeight);
		button.GetComponentInChildren<TMP_Text>(includeInactive: true).text = ConfigText.LobbyButton;
		button.onClick.AddListener(ConfigWindow.Toggle);
	}
}
