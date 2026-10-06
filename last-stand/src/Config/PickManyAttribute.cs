using System;

namespace LastStand.Config;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PickManyAttribute : Attribute
{
	public PickManyAttribute(ChoiceSet set)
	{
		Set = set;
	}

	public ChoiceSet Set { get; }
}
