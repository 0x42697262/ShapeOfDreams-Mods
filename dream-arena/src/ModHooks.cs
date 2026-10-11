using System;
using System.Collections.Generic;
using DreamArena;
using DreamArena.Config;
using DreamArena.Text;

namespace SodLib;

internal static partial class ModHooks
{
	public static partial IReadOnlyList<Choice> Choices(Enum set)
	{
		return ConfigChoices.For((ChoiceSet)set);
	}

	public static partial string LobbyName()
	{
		return Messages.LobbyName;
	}
}
