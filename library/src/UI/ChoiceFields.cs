using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SodLib.UI;

internal static class ChoiceFields
{
	private static readonly DewGUI.FieldBuilderCondition PickOneCondition = (type, info) =>
		type == typeof(string) && info != null && info.GetCustomAttribute<PickOneAttribute>() != null;

	private static readonly DewGUI.FieldBuilderCondition PickManyCondition = (type, info) =>
		type == typeof(string) && info != null && info.GetCustomAttribute<PickManyAttribute>() != null;

	public static void Register()
	{
		Unregister();
		DewGUI.fieldBuilders.Insert(0, (PickOneCondition, BuildDropdown));
		DewGUI.fieldBuilders.Insert(0, (PickManyCondition, BuildChecklist));
	}

	public static void Unregister()
	{
		DewGUI.fieldBuilders.RemoveAll(entry => entry.Item1 == PickOneCondition || entry.Item1 == PickManyCondition);
	}

	private static FieldBuildResult BuildDropdown(Type type, FieldInfo info, Transform parent)
	{
		IReadOnlyList<Choice> choices = ModHooks.Choices(info.GetCustomAttribute<PickOneAttribute>().Set);
		List<string> values = new List<string>();
		FieldBuildResult result = new FieldBuildResult();
		string current = null;

		TMP_Dropdown dropdown = UnityEngine.Object.Instantiate(DewGUI.widgetDropdown, parent).SetExpandWidth(1f);
		dropdown.onValueChanged.AddListener(index =>
		{
			if (index >= 0 && index < values.Count)
			{
				current = values[index];
				result.onChanged?.Invoke(current);
			}
		});

		result.getValue = () => current;
		result.setValue = value =>
		{
			current = value as string;
			values.Clear();
			List<string> labels = new List<string>();
			foreach (Choice choice in choices)
			{
				values.Add(choice.Value);
				labels.Add(Label(choice));
			}

			if (!string.IsNullOrEmpty(current) && !values.Contains(current))
			{
				values.Add(current);
				labels.Add(SharedConfigText.UnknownChoiceLabel(current));
			}

			dropdown.ClearOptions();
			dropdown.AddOptions(labels);
			dropdown.SetValueWithoutNotify(Math.Max(0, values.IndexOf(current)));
			dropdown.RefreshShownValue();
		};
		result.root = dropdown.gameObject;
		return result;
	}

	private static FieldBuildResult BuildChecklist(Type type, FieldInfo info, Transform parent)
	{
		IReadOnlyList<Choice> choices = ModHooks.Choices(info.GetCustomAttribute<PickManyAttribute>().Set);
		FieldBuildResult result = new FieldBuildResult();
		List<UI_Toggle> toggles = new List<UI_Toggle>();
		List<string> extras = new List<string>();
		bool updating = false;
		string current = null;

		VerticalLayoutGroup list = DewGUI.CreateVerticalLayoutGroup(parent, TextAnchor.UpperLeft);
		list.childForceExpandWidth = true;
		list.SetExpandWidth(1f);

		foreach (Choice choice in choices)
		{
			HorizontalLayoutGroup row = DewGUI.CreateHorizontalLayoutGroup(list.transform);
			UI_Toggle toggle = UnityEngine.Object.Instantiate(DewGUI.widgetToggleCheck, row.transform);
			UnityEngine.Object.Instantiate(DewGUI.widgetTextLabel, row.transform)
				.SetExpandWidth(expand: true)
				.SetText(Label(choice));
			toggle.onIsCheckedChanged.AddListener(_ =>
			{
				if (updating)
				{
					return;
				}

				current = Selected(choices, toggles, extras);
				result.onChanged?.Invoke(current);
			});
			toggles.Add(toggle);
		}

		result.getValue = () => current;
		result.setValue = value =>
		{
			current = value as string;
			List<string> picked = ChoiceList.Split(current);
			extras.Clear();
			extras.AddRange(picked.FindAll(item => !ChoiceList.Contains(choices, item)));

			updating = true;
			try
			{
				for (int i = 0; i < choices.Count; i++)
				{
					toggles[i].isChecked = picked.Contains(choices[i].Value);
				}
			}
			finally
			{
				updating = false;
			}
		};
		result.root = list.gameObject;
		return result;
	}

	private static string Selected(IReadOnlyList<Choice> choices, List<UI_Toggle> toggles, List<string> extras)
	{
		List<string> picked = new List<string>();
		for (int i = 0; i < choices.Count; i++)
		{
			if (toggles[i].isChecked)
			{
				picked.Add(choices[i].Value);
			}
		}

		picked.AddRange(extras);
		return ChoiceList.Join(picked);
	}

	private static string Label(Choice choice)
	{
		return SharedConfigText.ChoiceLabel(LocalizedNames.Find(choice.Value), choice.Detail);
	}
}
