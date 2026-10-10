using System;
using System.Collections.Generic;
using System.Reflection;
using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamArena.UI;

internal sealed class SettingsPanel
{
	public const float GrowWithContent = 0f;

	private static SettingsPage _lastPage = SettingsPage.Match;

	private readonly object _state;
	private readonly SafeAction _onChanged = new SafeAction();
	private readonly SafeAction _requestUpdate = new SafeAction();
	private readonly List<SettingsPage> _pages = new List<SettingsPage>();
	private readonly Dictionary<SettingsPage, GameObject> _pageRoots = new Dictionary<SettingsPage, GameObject>();
	private readonly Dictionary<SettingsPage, Button> _sidebarItems = new Dictionary<SettingsPage, Button>();

	private RectTransform _root;
	private Transform _sidebar;
	private Transform _content;
	private ScrollRect _scroll;
	private Transform _page;
	private Transform _card;

	private SettingsPanel(object state)
	{
		_state = state;
	}

	public static void Build(object state, Transform parent, float contentHeight, out SafeAction onChanged,
		out SafeAction requestUpdate)
	{
		SettingsPanel panel = new SettingsPanel(state);
		panel.BuildFrame(parent, contentHeight);
		panel.BuildFields();
		if (panel._pages.Count > 0)
		{
			panel.Show(panel._pages.Contains(_lastPage) ? _lastPage : panel._pages[0]);
		}

		onChanged = panel._onChanged;
		requestUpdate = panel._requestUpdate;
		panel._requestUpdate.Invoke();
	}

	private void BuildFrame(Transform parent, float contentHeight)
	{
		HorizontalLayoutGroup root = DewGUI.CreateHorizontalLayoutGroup(parent, TextAnchor.UpperLeft);
		root.spacing = SettingsLayout.SidebarGap;
		root.SetExpandWidth(expand: true);
		_root = (RectTransform)root.transform;

		VerticalLayoutGroup sidebar = DewGUI.CreateVerticalLayoutGroup(root.transform, TextAnchor.UpperLeft);
		sidebar.spacing = SettingsLayout.SidebarItemSpacing;
		sidebar.childForceExpandWidth = true;
		sidebar.padding = new RectOffset(SettingsLayout.SidebarPadding, SettingsLayout.SidebarPadding,
			SettingsLayout.SidebarPadding, SettingsLayout.SidebarPadding);
		sidebar.gameObject.AddComponent<Image>().color = SettingsLayout.SidebarBackground;
		FixWidth(sidebar.gameObject, SettingsLayout.SidebarWidth);
		_sidebar = sidebar.transform;

		if (contentHeight > GrowWithContent)
		{
			_scroll = UnityEngine.Object.Instantiate(DewGUI.widgetScrollRect, root.transform);
			_scroll.horizontal = false;
			_scroll.SetExpandWidth(expand: true);
			_scroll.SetHeight(contentHeight);
			sidebar.SetHeight(contentHeight);
			_content = _scroll.content;
			VerticalLayoutGroup scrolled = _content.gameObject.AddComponent<VerticalLayoutGroup>();
			StyleContent(scrolled);
			if (!_content.TryGetComponent(out ContentSizeFitter fitter))
			{
				fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
			}

			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			return;
		}

		VerticalLayoutGroup content = DewGUI.CreateVerticalLayoutGroup(root.transform, TextAnchor.UpperLeft);
		content.SetExpandWidth(expand: true);
		StyleContent(content);
		_content = content.transform;
	}

	private static void StyleContent(VerticalLayoutGroup content)
	{
		DewGUI.ApplyLayoutGroupDefaultSettings(content, TextAnchor.UpperLeft);
		content.childForceExpandWidth = true;
		content.padding = new RectOffset(0, SettingsLayout.PageRightPadding, 0, SettingsLayout.PageBottomPadding);
	}

	private void BuildFields()
	{
		foreach (FieldInfo field in ConfigFields.Visible(_state.GetType()))
		{
			DewGUI.FieldBuilder builder = BuilderFor(field);
			if (builder == null)
			{
				continue;
			}

			PageAttribute page = field.GetCustomAttribute<PageAttribute>();
			if (page != null || _page == null)
			{
				StartPage(page != null ? page.Page : SettingsPage.Match);
			}

			HeaderAttribute header = field.GetCustomAttribute<HeaderAttribute>();
			if (header != null || _card == null)
			{
				StartGroup(header?.header);
			}

			AddRow(field, builder);
		}
	}

	private static DewGUI.FieldBuilder BuilderFor(FieldInfo field)
	{
		foreach ((DewGUI.FieldBuilderCondition condition, DewGUI.FieldBuilder builder) in DewGUI.fieldBuilders)
		{
			if (condition(field.FieldType, field))
			{
				return builder;
			}
		}

		return null;
	}

	private void StartPage(SettingsPage page)
	{
		VerticalLayoutGroup root = DewGUI.CreateVerticalLayoutGroup(_content, TextAnchor.UpperLeft);
		root.spacing = SettingsLayout.PageSpacing;
		root.childForceExpandWidth = true;
		root.gameObject.name = page.ToString();
		_page = root.transform;
		_card = null;

		VerticalLayoutGroup heading = DewGUI.CreateVerticalLayoutGroup(_page, TextAnchor.UpperLeft);
		heading.spacing = SettingsLayout.PageTitleGap;
		heading.childForceExpandWidth = true;
		UnityEngine.Object.Instantiate(DewGUI.widgetTextHeader, heading.transform).SetText(ConfigText.PageTitle(page));
		TextMeshProUGUI subtitle = UnityEngine.Object.Instantiate(DewGUI.widgetTextBody, heading.transform);
		subtitle.SetText(ConfigText.PageSubtitle(page));
		subtitle.color = SettingsLayout.SecondaryText;

		_pages.Add(page);
		_pageRoots[page] = root.gameObject;
		_sidebarItems[page] = AddSidebarItem(page);
	}

	private Button AddSidebarItem(SettingsPage page)
	{
		GameObject item = new GameObject(page.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
		item.transform.SetParent(_sidebar, worldPositionStays: false);
		item.SetHeight(SettingsLayout.SidebarItemHeight);

		HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
		DewGUI.ApplyLayoutGroupDefaultSettings(layout, TextAnchor.MiddleLeft);
		layout.padding = new RectOffset(SettingsLayout.SidebarItemIndent, SettingsLayout.SidebarItemIndent, 0, 0);

		Image background = item.GetComponent<Image>();
		background.color = Color.white;

		TextMeshProUGUI label = UnityEngine.Object.Instantiate(DewGUI.widgetTextLabel, item.transform);
		label.SetText(ConfigText.PageTitle(page));
		label.raycastTarget = false;
		label.SetExpandWidth(expand: true);

		Button button = item.GetComponent<Button>();
		button.targetGraphic = background;
		button.transition = Selectable.Transition.ColorTint;
		button.onClick.AddListener(() => Show(page));
		return button;
	}

	private void StartGroup(string title)
	{
		if (!string.IsNullOrEmpty(title))
		{
			TextMeshProUGUI heading = UnityEngine.Object.Instantiate(DewGUI.widgetTextLabel, _page);
			heading.SetText(title);
			heading.color = SettingsLayout.SecondaryText;
			heading.fontSize *= SettingsLayout.GroupTitleFontScale;
			heading.margin = heading.margin.WithY(SettingsLayout.GroupTitleTopMargin);
		}

		VerticalLayoutGroup card = DewGUI.CreateVerticalLayoutGroup(_page, TextAnchor.UpperLeft);
		card.spacing = 0f;
		card.childForceExpandWidth = true;
		card.gameObject.AddComponent<Image>().color = SettingsLayout.CardBackground;
		_card = card.transform;
	}

	private void AddRow(FieldInfo field, DewGUI.FieldBuilder builder)
	{
		if (_card.childCount > 0)
		{
			AddSeparator();
		}

		HorizontalLayoutGroup row = DewGUI.CreateHorizontalLayoutGroup(_card, TextAnchor.MiddleLeft);
		StyleRow(row);
		Transform textParent = TextColumn(row.transform);
		HorizontalLayoutGroup control = DewGUI.CreateHorizontalLayoutGroup(row.transform, TextAnchor.MiddleRight);
		FixWidth(control.gameObject, SettingsLayout.ControlWidth);
		Transform controlParent = control.transform;

		ModConfig.LabelTextAttribute label = field.GetCustomAttribute<ModConfig.LabelTextAttribute>();
		UnityEngine.Object.Instantiate(DewGUI.widgetTextLabel, textParent)
			.SetText(label != null ? label.text : field.Name.NicifyVariableName());

		ModConfig.DescriptionAttribute description = field.GetCustomAttribute<ModConfig.DescriptionAttribute>();
		if (description != null)
		{
			TextMeshProUGUI text = UnityEngine.Object.Instantiate(DewGUI.widgetTextBody, textParent);
			text.SetText(description.text);
			text.color = SettingsLayout.SecondaryText;
			text.fontSize *= SettingsLayout.DescriptionFontScale;
		}

		FieldBuildResult result = builder(field.FieldType, field, controlParent);
		_requestUpdate.Add(() => result.setValue(field.GetValue(_state)));
		result.onChanged += value =>
		{
			field.SetValue(_state, value);
			try
			{
				_onChanged.Invoke();
			}
			catch (Exception e)
			{
				Log.Error(LogText.SettingChangeFailed, e);
			}
		};
	}

	private static void StyleRow(HorizontalOrVerticalLayoutGroup row)
	{
		row.spacing = SettingsLayout.RowSpacing;
		row.padding = new RectOffset(SettingsLayout.RowHorizontalPadding, SettingsLayout.RowHorizontalPadding,
			SettingsLayout.RowVerticalPadding, SettingsLayout.RowVerticalPadding);
	}

	private static Transform TextColumn(Transform row)
	{
		VerticalLayoutGroup column = DewGUI.CreateVerticalLayoutGroup(row, TextAnchor.UpperLeft);
		column.spacing = SettingsLayout.LabelSpacing;
		column.childForceExpandWidth = true;
		column.SetExpandWidth(expand: true);
		return column.transform;
	}

	private void AddSeparator()
	{
		GameObject separator = new GameObject(nameof(SettingsLayout.Separator), typeof(RectTransform), typeof(Image));
		separator.transform.SetParent(_card, worldPositionStays: false);
		separator.GetComponent<Image>().color = SettingsLayout.Separator;
		separator.GetComponent<Image>().raycastTarget = false;
		LayoutElement layout = separator.AddComponent<LayoutElement>();
		layout.minHeight = SettingsLayout.SeparatorHeight;
		layout.preferredHeight = SettingsLayout.SeparatorHeight;
	}

	private static void FixWidth(GameObject target, float width)
	{
		if (!target.TryGetComponent(out LayoutElement layout))
		{
			layout = target.AddComponent<LayoutElement>();
		}

		layout.minWidth = width;
		layout.preferredWidth = width;
		layout.flexibleWidth = 0f;
	}

	private void Show(SettingsPage page)
	{
		_lastPage = page;
		foreach (SettingsPage each in _pages)
		{
			bool selected = each == page;
			_pageRoots[each].SetActive(selected);
			Button item = _sidebarItems[each];
			ColorBlock colors = item.colors;
			colors.normalColor = selected ? SettingsLayout.SidebarSelected : SettingsLayout.SidebarIdle;
			colors.selectedColor = selected ? SettingsLayout.SidebarSelected : SettingsLayout.SidebarHover;
			colors.highlightedColor = selected ? SettingsLayout.SidebarSelected : SettingsLayout.SidebarHover;
			colors.pressedColor = SettingsLayout.SidebarPressed;
			item.colors = colors;
			TMP_Text label = item.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (label != null)
			{
				label.color = selected ? SettingsLayout.SidebarTextSelected : SettingsLayout.SidebarTextIdle;
			}
		}

		ScrollRect scroll = _scroll != null ? _scroll : _root.GetComponentInParent<ScrollRect>();
		LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
		if (scroll != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
			scroll.verticalNormalizedPosition = 1f;
		}
	}
}
