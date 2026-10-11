using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SodLib.UI;

internal sealed class SettingsPanel
{
	public const float GrowWithContent = 0f;

	private static Enum _lastPage;

	private readonly object _state;
	private readonly SafeAction _onChanged = new SafeAction();
	private readonly SafeAction _requestUpdate = new SafeAction();
	private readonly List<Enum> _pages = new List<Enum>();
	private readonly Dictionary<Enum, GameObject> _pageRoots = new Dictionary<Enum, GameObject>();
	private readonly Dictionary<Enum, Button> _sidebarItems = new Dictionary<Enum, Button>();

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
			panel.Show(_lastPage != null && panel._pages.Contains(_lastPage) ? _lastPage : panel._pages[0]);
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
		Enum firstPage = FirstPage(_state.GetType());
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
				StartPage(page != null ? page.Page : firstPage);
			}

			HeaderAttribute header = field.GetCustomAttribute<HeaderAttribute>();
			if (header != null || _card == null)
			{
				StartGroup(header?.header);
			}

			AddRow(field, builder);
		}
	}

	private static Enum FirstPage(Type type)
	{
		foreach (FieldInfo field in ConfigFields.Visible(type))
		{
			PageAttribute page = field.GetCustomAttribute<PageAttribute>();
			if (page != null)
			{
				return page.Page;
			}
		}

		return null;
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

	private void StartPage(Enum page)
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
		UnityEngine.Object.Instantiate(DewGUI.widgetTextHeader, heading.transform).SetText(ModText.PageTitle(page));
		TextMeshProUGUI subtitle = UnityEngine.Object.Instantiate(DewGUI.widgetTextBody, heading.transform);
		subtitle.SetText(ModText.PageSubtitle(page));
		subtitle.color = SettingsLayout.SecondaryText;

		_pages.Add(page);
		_pageRoots[page] = root.gameObject;
		_sidebarItems[page] = AddSidebarItem(page);
	}

	private Button AddSidebarItem(Enum page)
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
		label.SetText(ModText.PageTitle(page));
		label.raycastTarget = false;
		label.SetExpandWidth(expand: true);
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.fontSizeMax = label.fontSize;
		label.fontSizeMin = label.fontSize * SettingsLayout.SidebarMinTextScale;
		label.enableAutoSizing = true;

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

		bool stacked = field.GetCustomAttribute<PickManyAttribute>() != null;
		Transform controlParent;
		Transform textParent;
		if (stacked)
		{
			VerticalLayoutGroup row = DewGUI.CreateVerticalLayoutGroup(_card, TextAnchor.UpperLeft);
			StyleRow(row);
			row.childForceExpandWidth = true;
			textParent = TextColumn(row.transform);
			controlParent = row.transform;
		}
		else
		{
			HorizontalLayoutGroup row = DewGUI.CreateHorizontalLayoutGroup(_card, TextAnchor.MiddleLeft);
			StyleRow(row);
			textParent = TextColumn(row.transform);
			HorizontalLayoutGroup control = DewGUI.CreateHorizontalLayoutGroup(row.transform, TextAnchor.MiddleRight);
			FixWidth(control.gameObject, SettingsLayout.ControlWidth);
			controlParent = control.transform;
		}

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
				Log.Error(SharedLogText.SettingChangeFailed, e);
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

	private void Show(Enum page)
	{
		_lastPage = page;
		foreach (Enum each in _pages)
		{
			bool selected = Equals(each, page);
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
