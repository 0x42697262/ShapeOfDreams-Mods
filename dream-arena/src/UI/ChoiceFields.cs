using System;
using System.Collections.Generic;
using System.Reflection;
using DreamArena.Config;
using DreamArena.Game;
using DreamArena.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamArena.UI;

internal static class ChoiceFields
{
	private static readonly DewGUI.FieldBuilderCondition PickOneCondition = (type, info) =>
		type == typeof(string) && info != null && info.GetCustomAttribute<PickOneAttribute>() != null;

	public static void Register()
	{
		Unregister();
		DewGUI.fieldBuilders.Insert(0, (PickOneCondition, BuildDropdown));
	}

	public static void Unregister()
	{
		DewGUI.fieldBuilders.RemoveAll(entry => entry.Item1 == PickOneCondition);
	}

	private static FieldBuildResult BuildDropdown(Type type, FieldInfo info, Transform parent)
	{
		IReadOnlyList<Choice> choices = ConfigChoices.For(info.GetCustomAttribute<PickOneAttribute>().Set);
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
				labels.Add(ConfigText.UnknownChoiceLabel(current));
			}

			dropdown.ClearOptions();
			dropdown.AddOptions(labels);
			dropdown.SetValueWithoutNotify(Math.Max(0, values.IndexOf(current)));
			dropdown.RefreshShownValue();
		};
		result.root = dropdown.gameObject;
		return result;
	}

	private static string Label(Choice choice)
	{
		return ConfigText.ChoiceLabel(LocalizedNames.Find(choice.Value), choice.Detail);
	}
}
