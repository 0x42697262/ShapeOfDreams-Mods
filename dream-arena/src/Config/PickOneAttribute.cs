using System;

namespace DreamArena.Config;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PickOneAttribute : Attribute
{
	public PickOneAttribute(ChoiceSet set)
	{
		Set = set;
	}

	public ChoiceSet Set { get; }
}
