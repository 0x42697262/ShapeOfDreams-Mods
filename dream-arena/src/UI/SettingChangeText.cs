using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DreamArena.Config;
using DreamArena.Game;
using DreamArena.Text;

namespace DreamArena.UI;

internal static class SettingChangeText
{
	public static string Describe(SettingChange change)
	{
		FieldInfo field = change.Field;
		ModConfig.LabelTextAttribute label = field.GetCustomAttribute<ModConfig.LabelTextAttribute>();
		string name = label != null ? label.text : field.Name;

		PickOneAttribute one = field.GetCustomAttribute<PickOneAttribute>();
		IReadOnlyList<Choice> choices = one != null ? ConfigChoices.For(one.Set) : null;
		return Messages.SettingChangeLine(name, Value(change.Before, choices), Value(change.After, choices));
	}

	private static string Value(object value, IReadOnlyList<Choice> choices)
	{
		switch (value)
		{
			case bool flag:
				return flag ? Messages.SettingOn : Messages.SettingOff;
			case float number:
				return number.ToString(Messages.SettingNumberFormat, CultureInfo.InvariantCulture);
			case string text when choices != null:
				return ChoiceName(text, choices);
			case null:
				return string.Empty;
			default:
				return Convert.ToString(value, CultureInfo.InvariantCulture);
		}
	}

	private static string ChoiceName(string value, IReadOnlyList<Choice> choices)
	{
		string localized = LocalizedNames.Find(value);
		if (localized != null)
		{
			return localized;
		}

		if (choices != null)
		{
			foreach (Choice choice in choices)
			{
				if (string.Equals(choice.Value, value, StringComparison.Ordinal))
				{
					return choice.Detail;
				}
			}
		}

		return value;
	}
}
