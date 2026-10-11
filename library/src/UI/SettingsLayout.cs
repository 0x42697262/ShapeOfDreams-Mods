using UnityEngine;

namespace SodLib.UI;

internal static class SettingsLayout
{
	public const float SidebarWidth = 280f;

	public const float SidebarMinTextScale = 0.75f;
	public const float SidebarGap = 28f;
	public const int SidebarPadding = 12;
	public const float SidebarItemSpacing = 4f;
	public const float SidebarItemHeight = 48f;
	public const int SidebarItemIndent = 18;

	public const float PageSpacing = 10f;
	public const int PageRightPadding = 24;
	public const int PageBottomPadding = 24;
	public const float PageTitleGap = 6f;

	public const int GroupTitleTopMargin = 14;
	public const float GroupTitleFontScale = 0.9f;

	public const int RowHorizontalPadding = 20;
	public const int RowVerticalPadding = 14;
	public const float RowSpacing = 24f;
	public const float LabelSpacing = 2f;
	public const float ControlWidth = 300f;
	public const float DescriptionFontScale = 0.85f;
	public const float SeparatorHeight = 1f;

	public static readonly Color SidebarBackground = new Color(1f, 1f, 1f, 0.03f);
	public static readonly Color SidebarIdle = new Color(1f, 1f, 1f, 0f);
	public static readonly Color SidebarHover = new Color(1f, 1f, 1f, 0.07f);
	public static readonly Color SidebarPressed = new Color(1f, 1f, 1f, 0.12f);
	public static readonly Color SidebarSelected = new Color(1f, 1f, 1f, 0.15f);
	public static readonly Color SidebarTextIdle = new Color(1f, 1f, 1f, 0.6f);
	public static readonly Color SidebarTextSelected = Color.white;

	public static readonly Color CardBackground = new Color(1f, 1f, 1f, 0.045f);
	public static readonly Color Separator = new Color(1f, 1f, 1f, 0.07f);
	public static readonly Color SecondaryText = new Color(1f, 1f, 1f, 0.55f);
}
