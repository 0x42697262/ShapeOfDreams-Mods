using System;

namespace SodLib.Config;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PickOneAttribute : Attribute
{
	public PickOneAttribute(object set)
	{
		Set = (Enum)set;
	}

	public Enum Set { get; }
}
