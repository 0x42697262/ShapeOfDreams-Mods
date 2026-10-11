using System;
using LastStand.Config;
using LastStand.Identity;
using LastStand.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastStand.UI;

internal sealed class ConfigWindow : MonoBehaviour
{
	private static ConfigWindow _current;

	private LastStandConfig _draft;
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

		LastStandMod mod = LastStandMod.Current;
		if (mod == null)
		{
			return;
		}

		UI_Window window = UnityEngine.Object.Instantiate(DewGUI.widgetWindow, DewGUI.canvasTransform);
		window.name = ModIdentity.ConfigWindowName;
		window.isDraggable = true;
		window.enableBackdrop = true;

		_current = window.gameObject.AddComponent<ConfigWindow>();
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

	private void Build(UI_Window window, LastStandMod mod)
	{
		_globalUIManager = ManagerBase<GlobalUIManager>.softInstance;
		if (_globalUIManager != null)
		{
			_backHandler = _globalUIManager.AddBackHandler(this, int.MaxValue, OnBack);
		}

		_draft = (LastStandConfig)mod.config.Clone();

		VerticalLayoutGroup content = DewGUI.CreateVerticalLayoutGroup(window.transform);
		content.padding = new RectOffset(ConfigWindowLayout.WindowPadding, ConfigWindowLayout.WindowPadding,
			ConfigWindowLayout.WindowVerticalPadding, ConfigWindowLayout.WindowVerticalPadding);
		content.spacing = ConfigWindowLayout.RowSpacing;
		content.childForceExpandWidth = true;

		TMP_Text header = UnityEngine.Object.Instantiate(DewGUI.widgetTextHeader, content.transform);
		header.alignment = TextAlignmentOptions.Center;
		header.text = ConfigText.WindowTitle;

		TMP_Text note = UnityEngine.Object.Instantiate(DewGUI.widgetTextBody, content.transform);
		note.alignment = TextAlignmentOptions.Center;
		note.text = ConfigText.WindowNote;

		float canvasWidth = DewGUI.canvasTransform.rect.width;
		float windowWidth = canvasWidth > 0f
			? Mathf.Min(ConfigWindowLayout.PreferredWindowWidth, canvasWidth - ConfigWindowLayout.ScreenEdgeMargin)
			: ConfigWindowLayout.PreferredWindowWidth;
		window.SetWidth(windowWidth);

		SettingsPanel.Build(_draft, content.transform, ConfigWindowLayout.PageHeight, out SafeAction _, out SafeAction _);

		HorizontalLayoutGroup buttons = DewGUI.CreateHorizontalLayoutGroup(content.transform, TextAnchor.MiddleCenter);
		buttons.spacing = ConfigWindowLayout.ButtonSpacing;
		buttons.padding = new RectOffset(0, 0, ConfigWindowLayout.ButtonTopPadding, 0);

		AddButton(buttons.transform, ConfigText.ApplyButton, Apply);
		AddButton(buttons.transform, ConfigText.CancelButton, Close);
	}

	private static void AddButton(Transform parent, string label, Action onClick)
	{
		Button button = UnityEngine.Object.Instantiate(DewGUI.widgetButton, parent);
		button.SetWidth(ConfigWindowLayout.ButtonWidth);
		button.SetHeight(ConfigWindowLayout.ButtonHeight);
		button.GetComponentInChildren<TMP_Text>(includeInactive: true).text = label;
		button.onClick.AddListener(() => onClick());
	}

	private void Apply()
	{
		LastStandMod.Current?.ApplyConfig(_draft);
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
