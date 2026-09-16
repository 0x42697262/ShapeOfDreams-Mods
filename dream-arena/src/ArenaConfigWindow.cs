using System;
using UnityEngine;
using UnityEngine.UI;

namespace DreamArena;

/// <summary>
/// The host's in-lobby settings window.
/// </summary>
/// <remarks>
/// Widgets are generated from <see cref="DreamArenaConfig"/> by
/// <c>DewGUI.CreateWidgetsForObject</c> — the same call the mod manager's
/// config screen uses — so bools become toggles and the team-mode enum
/// becomes a dropdown with no custom UI code, and adding a field to the config
/// automatically adds a widget here.
///
/// Edits are made against a scratch copy and only committed on Apply.
/// </remarks>
internal sealed class ArenaConfigWindow : MonoBehaviour
{
	private static ArenaConfigWindow _current;

	private DreamArenaConfig _draft;
	private GlobalUIManager _globalUIManager;
	private BackHandler _backHandler;

	public static bool IsOpen => _current != null;

	public static void Toggle()
	{
		if (IsOpen)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public static void Open()
	{
		if (_current != null)
		{
			_current.transform.SetAsLastSibling();
			return;
		}

		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}cannot open config: mod is not loaded.");
			return;
		}

		UI_Window window = UnityEngine.Object.Instantiate(DewGUI.widgetWindow, DewGUI.canvasTransform);
		window.name = ModIdentity.ConfigWindowName;
		window.isDraggable = true;
		window.enableBackdrop = true;
		window.SetWidth(720f);

		_current = window.gameObject.AddComponent<ArenaConfigWindow>();
		_current.Build(window, mod);
	}

	public static void Close()
	{
		if (_current == null)
		{
			return;
		}

		UnityEngine.Object.Destroy(_current.gameObject);
		_current = null;
	}

	private void Build(UI_Window window, DreamArenaMod mod)
	{
		_globalUIManager = ManagerBase<GlobalUIManager>.instance;
		if (_globalUIManager != null)
		{
			_backHandler = _globalUIManager.AddBackHandler(this, int.MaxValue, OnBack);
		}

		// Edit a copy so Cancel is a real cancel.
		_draft = new DreamArenaConfig();
		mod.config.CopyValuesTo(_draft);

		VerticalLayoutGroup content = DewGUI.CreateVerticalLayoutGroup(window.transform);
		content.name = "Arena Config Content";
		content.padding = new RectOffset(36, 36, 28, 28);
		content.spacing = 12f;
		content.childForceExpandWidth = true;

		TMPro.TMP_Text header = UnityEngine.Object.Instantiate(DewGUI.widgetTextHeader, content.transform);
		header.alignment = TMPro.TextAlignmentOptions.Center;
		header.text = "Dream Arena";

		TMPro.TMP_Text note = UnityEngine.Object.Instantiate(DewGUI.widgetTextBody, content.transform);
		note.alignment = TMPro.TextAlignmentOptions.Center;
		note.text = "Host only. Other players do not need this mod.";

		// The whole settings surface, generated from the config type.
		DewGUI.CreateWidgetsForObject(typeof(DreamArenaConfig), _draft, content.transform,
			out SafeAction _, out SafeAction _);

		HorizontalLayoutGroup buttons =
			DewGUI.CreateHorizontalLayoutGroup(content.transform, TextAnchor.MiddleCenter);
		buttons.name = "Arena Config Buttons";
		buttons.spacing = 16f;
		buttons.padding = new RectOffset(0, 0, 10, 0);

		AddButton(buttons.transform, "Apply", Apply);
		AddButton(buttons.transform, "Cancel", Close);
	}

	private static void AddButton(Transform parent, string label, Action onClick)
	{
		Button button = UnityEngine.Object.Instantiate(DewGUI.widgetButton, parent);
		button.SetWidth(200f);
		button.SetHeight(56f);
		button.GetComponentInChildren<TMPro.TMP_Text>(includeInactive: true).text = label;
		button.onClick.AddListener(() => onClick());
	}

	private void Apply()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null)
		{
			Close();
			return;
		}

		_draft.CopyValuesTo(mod.config);
		mod.ApplyAndPersist();
		Close();
	}

	private bool OnBack()
	{
		Close();
		return true;
	}

	private void OnDestroy()
	{
		if (_globalUIManager != null && _backHandler != null)
		{
			_globalUIManager.RemoveBackHandler(_backHandler);
			_backHandler = null;
		}

		if (_current == this)
		{
			_current = null;
		}
	}
}
