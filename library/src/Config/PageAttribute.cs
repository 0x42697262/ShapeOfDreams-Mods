using System;

namespace SodLib.Config;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PageAttribute : Attribute
{
	public PageAttribute(object page)
	{
		Page = (Enum)page;
	}

	public Enum Page { get; }
}
