using System;

namespace SodLib.Config;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PickManyAttribute : Attribute
{
	public PickManyAttribute(object set)
	{
		Set = (Enum)set;
	}

	public Enum Set { get; }
}
