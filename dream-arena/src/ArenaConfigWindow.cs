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
	private const int ColumnCount = 3;
	private const float PreferredWindowWidth = 1600f;
	private const float ScreenEdgeMargin = 140f;
	private const float ColumnSpacing = 56f;
	private const float FallbackColumnWidth = 400f;
	private const int ScrollPadding = 20;
	private const int WindowPadding = 36;
	private const float ScrollbarGap = 16f;
	private const float MaxScrollHeight = 760f;

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
		content.padding = new RectOffset(WindowPadding, WindowPadding, 28, 28);
		content.spacing = 12f;
		content.childForceExpandWidth = true;

		TMPro.TMP_Text header = UnityEngine.Object.Instantiate(DewGUI.widgetTextHeader, content.transform);
		header.alignment = TMPro.TextAlignmentOptions.Center;
		header.text = "Dream Arena";

		TMPro.TMP_Text note = UnityEngine.Object.Instantiate(DewGUI.widgetTextBody, content.transform);
		note.alignment = TMPro.TextAlignmentOptions.Center;
		note.text = "Host only. Other players do not need this mod.";

		// Settings sit in a fixed-height scroll area, as in the game's own mod
		// config screen. Without it the window grows to fit, gets capped at the
		// screen height, and the layout squashes the rows on top of each other.
		ScrollRect scroll = UnityEngine.Object.Instantiate(DewGUI.widgetScrollRect, content.transform);
		scroll.name = "Arena Config Scroll";
		scroll.SetExpandWidth(expand: true);
		scroll.horizontal = false;

		RectTransform scrollContent = scroll.content;
		HorizontalLayoutGroup row = scrollContent.gameObject.AddComponent<HorizontalLayoutGroup>();
		DewGUI.ApplyLayoutGroupDefaultSettings(row, TextAnchor.UpperLeft);
		row.spacing = ColumnSpacing;
		row.padding = new RectOffset(ScrollPadding, ScrollPadding, 8, 8);
		if (!scrollContent.TryGetComponent(out ContentSizeFitter fitter))
		{
			fitter = scrollContent.gameObject.AddComponent<ContentSizeFitter>();
		}
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		var columns = new Transform[ColumnCount];
		for (int i = 0; i < ColumnCount; i++)
		{
			VerticalLayoutGroup column = DewGUI.CreateVerticalLayoutGroup(scrollContent, TextAnchor.UpperLeft);
			column.name = $"Arena Config Column {i}";
			column.spacing = 12f;
			column.childForceExpandWidth = true;
			columns[i] = column.transform;
		}

		// Size the window first, capped below the screen width (the game caps
		// it at the screen anyway), then measure the scroll area it actually
		// produced. Assuming a width here is what clipped the last column: the
		// window frame and the scrollbar both take space we cannot see from
		// code.
		float canvasWidth = DewGUI.canvasTransform.rect.width;
		float windowWidth = canvasWidth > 0f
			? Mathf.Min(PreferredWindowWidth, canvasWidth - ScreenEdgeMargin)
			: PreferredWindowWidth;
		window.SetWidth(windowWidth);
		scroll.SetHeight(MaxScrollHeight);
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)window.transform);

		float columnWidth = ColumnWidthFor(scroll);
		foreach (Transform column in columns)
		{
			column.SetWidth(columnWidth);
			LayoutElement layout = column.GetComponent<LayoutElement>();
			layout.minWidth = columnWidth;
			layout.flexibleWidth = 0f;
		}

		// The whole settings surface, generated from the config type into the
		// first column, then dealt out across the others section by section.
		DewGUI.CreateWidgetsForObject(typeof(DreamArenaConfig), _draft, columns[0],
			out SafeAction _, out SafeAction _);
		float tallest = SpreadIntoColumns(scrollContent, columns);
		scroll.SetHeight(Mathf.Clamp(tallest + 20f, 200f, MaxScrollHeight));

		HorizontalLayoutGroup buttons =
			DewGUI.CreateHorizontalLayoutGroup(content.transform, TextAnchor.MiddleCenter);
		buttons.name = "Arena Config Buttons";
		buttons.spacing = 16f;
		buttons.padding = new RectOffset(0, 0, 10, 0);

		AddButton(buttons.transform, "Apply", Apply);
		AddButton(buttons.transform, "Cancel", Close);
	}

	/// <summary>
	/// Splits the scroll area's real inner width evenly between the columns,
	/// leaving room for the padding, the gaps and the scrollbar.
	/// </summary>
	private static float ColumnWidthFor(ScrollRect scroll)
	{
		RectTransform viewport = scroll.viewport != null
			? scroll.viewport
			: (RectTransform)scroll.transform;
		float available = viewport.rect.width;

		Scrollbar bar = scroll.verticalScrollbar;
		if (bar != null && scroll.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport)
		{
			// The bar overlays the viewport rather than shrinking it.
			available -= ((RectTransform)bar.transform).rect.width;
		}

		available -= 2 * ScrollPadding + (ColumnCount - 1) * ColumnSpacing + ScrollbarGap;
		float width = available / ColumnCount;
		return width > 120f ? Mathf.Floor(width) : FallbackColumnWidth;
	}

	/// <summary>
	/// Moves the generated widgets out of the first column so the columns come
	/// out about equally tall, and returns the tallest column's height.
	/// </summary>
	/// <remarks>
	/// <c>CreateWidgetsForObject</c> emits, per field, an optional section header,
	/// the label-and-control row, then an optional description. A section (a
	/// header plus the fields under it) is never split, and sections keep their
	/// order, reading down each column in turn. Heights are measured after a
	/// forced layout pass at the real column width, so wrapped descriptions
	/// count at their true size.
	/// </remarks>
	private static float SpreadIntoColumns(RectTransform scrollContent, Transform[] columns)
	{
		Transform source = columns[0];
		LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);

		var sections = new System.Collections.Generic.List<System.Collections.Generic.List<Transform>>();
		var heights = new System.Collections.Generic.List<float>();
		for (int i = 0; i < source.childCount; i++)
		{
			Transform child = source.GetChild(i);
			bool startsSection = child.name.Contains("Header") || sections.Count == 0;
			if (startsSection)
			{
				sections.Add(new System.Collections.Generic.List<Transform>());
				heights.Add(0f);
			}

			sections[sections.Count - 1].Add(child);
			float height = child is RectTransform rect ? LayoutUtility.GetPreferredHeight(rect) : 0f;
			heights[heights.Count - 1] += Mathf.Max(height, 1f) + 12f;
		}

		int[] firstOfColumn = SplitEvenly(heights, columns.Length);
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

	/// <summary>
	/// Splits consecutive section heights into <paramref name="count"/> runs with
	/// the smallest possible tallest run. Returns the first section index of each
	/// run. There are only a handful of sections, so every split is tried.
	/// </summary>
	private static int[] SplitEvenly(System.Collections.Generic.List<float> heights, int count)
	{
		int n = heights.Count;
		var best = new int[count];
		var current = new int[count];
		float bestTallest = float.MaxValue;

		void Search(int run, int start)
		{
			current[run] = start;
			if (run == count - 1)
			{
				float tallest = 0f;
				for (int r = 0; r < count; r++)
				{
					int end = r + 1 < count ? current[r + 1] : n;
					float sum = 0f;
					for (int i = current[r]; i < end; i++)
					{
						sum += heights[i];
					}
					tallest = Mathf.Max(tallest, sum);
				}

				if (tallest < bestTallest)
				{
					bestTallest = tallest;
					current.CopyTo(best, 0);
				}
				return;
			}

			// Leave at least one section for every later run when possible.
			for (int next = start + 1; next <= Mathf.Max(start + 1, n - (count - run - 1)); next++)
			{
				Search(run + 1, Mathf.Min(next, n));
			}
		}

		Search(0, 0);
		return best;
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
