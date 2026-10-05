using System.Reflection;
using HarmonyLib;
using LastStand.Game;
using LastStand.Identity;
using LastStand.Mode;
using LastStand.Text;

namespace LastStand.UI;

internal static class LobbyModeOption
{
	private static readonly FieldInfo StateField =
		AccessTools.Field(typeof(UI_Title_CreateLobbyView), GameNames.CreateLobbyState);

	public static void Add(UI_Title_CreateLobbyView view)
	{
		view.gameModeDropdown.AddOption(Messages.ModeName, ModIdentity.LobbyModeValue);
	}

	public static void Sync(UI_Title_CreateLobbyView view)
	{
		bool selected = ModIdentity.LobbyModeValue.Equals(view.gameModeDropdown.value as string);

		if (selected && view.gameModeDescriptionText != null)
		{
			view.gameModeDescriptionText.text = Messages.ModeDescription;
		}

		if (!(StateField?.GetValue(view) is DewNetworkStartSettings state))
		{
			return;
		}

		if (selected)
		{
			ModeSelection.Select(state);
		}
		else
		{
			ModeSelection.Deselect(state);
		}
	}
}
