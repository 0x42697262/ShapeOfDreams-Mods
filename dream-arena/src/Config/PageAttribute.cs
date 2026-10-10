using System;

namespace DreamArena.Config;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PageAttribute : Attribute
{
	public PageAttribute(SettingsPage page)
	{
		Page = page;
	}

	public SettingsPage Page { get; }
}
