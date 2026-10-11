using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SodLib.Config;

internal static class ConfigFields
{
	public static IEnumerable<FieldInfo> Visible(Type type)
	{
		foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if ((field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
			    && field.GetCustomAttribute<HideInInspector>() == null)
			{
				yield return field;
			}
		}
	}
}
