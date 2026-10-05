using System.Collections.Generic;
using LastStand.Game;
using LastStand.Identity;
using LastStand.Mode;
using LastStand.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastStand.UI;

internal static class SoloModeButton
{
	private const int NextSibling = 1;

	public static bool IsLimboAvailable()
	{
		return Dew.IsGameModifierIncludedInGame(GameNames.LimboModifier);
	}

	public static void Ensure(UI_Title_TitleView view)
	{
		if (view == null || view.limboModeText == null)
		{
			return;
		}

		Button template = view.limboModeText.GetComponentInParent<Button>(true);
		if (template == null)
		{
			return;
		}

		template.gameObject.SetActive(IsLimboAvailable());

		Transform panel = template.transform.parent;
		if (panel.Find(ModIdentity.SoloButtonName) != null)
		{
			return;
		}

		List<int> labelPath = PathFrom(template.transform, view.limboModeText.transform);

		GameObject clone = Object.Instantiate(template.gameObject, panel);
		clone.name = ModIdentity.SoloButtonName;
		clone.SetActive(true);
		clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + NextSibling);

		foreach (DewLocalizedText localized in clone.GetComponentsInChildren<DewLocalizedText>(true))
		{
			Object.DestroyImmediate(localized);
		}

		TextMeshProUGUI label = Follow(clone.transform, labelPath).GetComponent<TextMeshProUGUI>();
		foreach (TextMeshProUGUI text in clone.GetComponentsInChildren<TextMeshProUGUI>(true))
		{
			text.text = text == label ? Messages.ModeName : Messages.ModeTooltip;
		}

		Button button = clone.GetComponent<Button>();
		button.onClick = new Button.ButtonClickedEvent();
		button.onClick.AddListener(ModeSelection.StartSolo);
	}

	private static List<int> PathFrom(Transform root, Transform target)
	{
		List<int> path = new List<int>();
		for (Transform current = target; current != null && current != root; current = current.parent)
		{
			path.Insert(0, current.GetSiblingIndex());
		}

		return path;
	}

	private static Transform Follow(Transform root, List<int> path)
	{
		Transform current = root;
		foreach (int index in path)
		{
			current = current.GetChild(index);
		}

		return current;
	}
}
