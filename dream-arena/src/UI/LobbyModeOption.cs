using System;
using System.Reflection;
using HarmonyLib;
using DreamArena.Game;
using DreamArena.Identity;
using DreamArena.Mode;
using DreamArena.Text;

namespace DreamArena.UI;

internal static class LobbyModeOption
{
	private static readonly FieldInfo StateField =
		AccessTools.Field(typeof(UI_Title_CreateLobbyView), GameNames.CreateLobbyState);

	private static UI_Title_CreateLobbyView _view;

	public static void Add(UI_Title_CreateLobbyView view)
	{
		Detach();
		view.gameModeDropdown.AddOption(Messages.ModeName, ModIdentity.LobbyModeValue);
		_view = view;
		view.gameModeDropdown.onValueChanged += (Action<object>)OnModeChanged;
	}

	public static void Detach()
	{
		if (_view != null && _view.gameModeDropdown != null)
		{
			_view.gameModeDropdown.onValueChanged -= (Action<object>)OnModeChanged;
		}

		_view = null;
	}

	private static void OnModeChanged(object value)
	{
		if (_view == null || _view.lobbyNameInputField == null || !ModIdentity.LobbyModeValue.Equals(value as string))
		{
			return;
		}

		_view.lobbyNameInputField.text = Messages.LobbyName;
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
