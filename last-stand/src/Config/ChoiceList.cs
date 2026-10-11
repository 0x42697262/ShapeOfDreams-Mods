using System;
using System.Collections.Generic;

namespace LastStand.Config;

internal static class ChoiceList
{
	public static List<string> Split(string text)
	{
		List<string> items = new List<string>();
		if (string.IsNullOrEmpty(text))
		{
			return items;
		}

		foreach (string part in text.Split(Tuning.ListSeparator))
		{
			string item = part.Trim();
			if (item.Length > 0 && !items.Contains(item))
			{
				items.Add(item);
			}
		}

		return items;
	}

	public static string Join(IEnumerable<string> items)
	{
		return string.Join(Tuning.ListJoiner, items);
	}

	public static bool Contains(IReadOnlyList<Choice> choices, string value)
	{
		foreach (Choice choice in choices)
		{
			if (string.Equals(choice.Value, value, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}
}
