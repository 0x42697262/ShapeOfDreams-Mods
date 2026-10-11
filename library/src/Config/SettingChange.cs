using System.Collections.Generic;
using System.Reflection;

namespace SodLib.Config;

internal readonly struct SettingChange
{
	public SettingChange(FieldInfo field, object before, object after)
	{
		Field = field;
		Before = before;
		After = after;
	}

	public FieldInfo Field { get; }

	public object Before { get; }

	public object After { get; }

	public static List<SettingChange> Between(ModConfig before, ModConfig after)
	{
		List<SettingChange> changes = new List<SettingChange>();
		foreach (FieldInfo field in ConfigFields.Visible(after.GetType()))
		{
			object old = field.GetValue(before);
			object current = field.GetValue(after);
			if (!Equals(old, current))
			{
				changes.Add(new SettingChange(field, old, current));
			}
		}

		return changes;
	}
}
