using System;
using System.Collections.Generic;
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

		ScrollRect scroll = UnityEngine.Object.Instantiate(DewGUI.widgetScrollRect, content.transform);
		scroll.SetExpandWidth(expand: true);
		scroll.horizontal = false;

		RectTransform scrollContent = scroll.content;
		HorizontalLayoutGroup row = scrollContent.gameObject.AddComponent<HorizontalLayoutGroup>();
		DewGUI.ApplyLayoutGroupDefaultSettings(row, TextAnchor.UpperLeft);
		row.spacing = ConfigWindowLayout.ColumnSpacing;
		row.padding = new RectOffset(ConfigWindowLayout.ScrollPadding, ConfigWindowLayout.ScrollPadding,
			ConfigWindowLayout.ScrollVerticalPadding, ConfigWindowLayout.ScrollVerticalPadding);
		if (!scrollContent.TryGetComponent(out ContentSizeFitter fitter))
		{
			fitter = scrollContent.gameObject.AddComponent<ContentSizeFitter>();
		}

		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		Transform[] columns = new Transform[ConfigWindowLayout.ColumnCount];
		for (int i = 0; i < columns.Length; i++)
		{
			VerticalLayoutGroup column = DewGUI.CreateVerticalLayoutGroup(scrollContent, TextAnchor.UpperLeft);
			column.spacing = ConfigWindowLayout.RowSpacing;
			column.childForceExpandWidth = true;
			columns[i] = column.transform;
		}

		float canvasWidth = DewGUI.canvasTransform.rect.width;
		float windowWidth = canvasWidth > 0f
			? Mathf.Min(ConfigWindowLayout.PreferredWindowWidth, canvasWidth - ConfigWindowLayout.ScreenEdgeMargin)
			: ConfigWindowLayout.PreferredWindowWidth;
		window.SetWidth(windowWidth);
		scroll.SetHeight(ConfigWindowLayout.MaxScrollHeight);
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)window.transform);

		float columnWidth = ColumnWidthFor(scroll);
		foreach (Transform column in columns)
		{
			column.SetWidth(columnWidth);
			LayoutElement layout = column.GetComponent<LayoutElement>();
			if (layout == null)
			{
				layout = column.gameObject.AddComponent<LayoutElement>();
			}

			layout.minWidth = columnWidth;
			layout.flexibleWidth = 0f;
		}

		_draft.BuildWidgets(columns[0], out SafeAction _, out SafeAction _);
		float tallest = SpreadIntoColumns(scrollContent, columns);
		scroll.SetHeight(Mathf.Clamp(tallest + ConfigWindowLayout.ScrollHeightSlack,
			ConfigWindowLayout.MinScrollHeight, ConfigWindowLayout.MaxScrollHeight));

		HorizontalLayoutGroup buttons = DewGUI.CreateHorizontalLayoutGroup(content.transform, TextAnchor.MiddleCenter);
		buttons.spacing = ConfigWindowLayout.ButtonSpacing;
		buttons.padding = new RectOffset(0, 0, ConfigWindowLayout.ButtonTopPadding, 0);

		AddButton(buttons.transform, ConfigText.ApplyButton, Apply);
		AddButton(buttons.transform, ConfigText.CancelButton, Close);
	}

	private static float ColumnWidthFor(ScrollRect scroll)
	{
		RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
		float available = viewport.rect.width;

		Scrollbar bar = scroll.verticalScrollbar;
		if (bar != null && scroll.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
		{
			available -= ((RectTransform)bar.transform).rect.width;
		}

		available -= 2 * ConfigWindowLayout.ScrollPadding
		             + (ConfigWindowLayout.ColumnCount - 1) * ConfigWindowLayout.ColumnSpacing
		             + ConfigWindowLayout.ScrollbarGap;
		float width = available / ConfigWindowLayout.ColumnCount;
		return width > ConfigWindowLayout.MinimumColumnWidth ? Mathf.Floor(width) : ConfigWindowLayout.FallbackColumnWidth;
	}

	private static float SpreadIntoColumns(RectTransform scrollContent, Transform[] columns)
	{
		Transform source = columns[0];
		LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

		List<List<Transform>> sections = new List<List<Transform>>();
		List<float> heights = new List<float>();
		for (int i = 0; i < source.childCount; i++)
		{
			Transform child = source.GetChild(i);
			if (child.name.Contains(ConfigWindowLayout.HeaderMarker) || sections.Count == 0)
			{
				sections.Add(new List<Transform>());
				heights.Add(0f);
			}

			sections[sections.Count - 1].Add(child);
			float height = child is RectTransform rect ? LayoutUtility.GetPreferredHeight(rect) : 0f;
			heights[heights.Count - 1] += Mathf.Max(height, ConfigWindowLayout.MinimumElementHeight)
			                              + ConfigWindowLayout.RowSpacing;
		}

		int[] firstOfColumn = ColumnSplit.Evenly(heights, columns.Length);
		float tallest = 0f;
		int column = 0;
		float columnHeight = 0f;
		for (int s = 0; s < sections.Count; s++)
		{
			while (column + 1 < columns.Length && s >= firstOfColumn[column + 1])
			{
				column++;
				columnHeight = 0f;
			}

			foreach (Transform element in sections[s])
			{
				element.SetParent(columns[column], worldPositionStays: false);
				element.SetAsLastSibling();
			}

			columnHeight += heights[s];
			tallest = Mathf.Max(tallest, columnHeight);
		}

		return tallest;
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
