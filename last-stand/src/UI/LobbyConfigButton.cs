using LastStand.Identity;
using LastStand.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastStand.UI;

internal static class LobbyConfigButton
{
	public static void Ensure(Transform panel)
	{
		if (panel == null || panel.Find(ModIdentity.LobbyButtonName) != null)
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
