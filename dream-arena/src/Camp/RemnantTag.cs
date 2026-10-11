using System;
using DreamArena.Identity;

namespace DreamArena.Camp;

internal static class RemnantTag
{
	private const char Separator = '|';

	private const int TagParts = 2;

	public static void Write(ChoiceShrine remnant, RemnantKind kind, Rarity rarity)
	{
		remnant.name = ModIdentity.RemnantName(kind.ToString() + Separator + rarity);
	}

	public static bool TryRead(ChoiceShrine remnant, out (RemnantKind Kind, Rarity Rarity) tag)
	{
		tag = default;
		if (remnant == null || !ModIdentity.IsOurs(remnant.name))
		{
			return false;
		}

		string body = remnant.name.Substring(0, remnant.name.Length - ModIdentity.OwnedSuffix.Length);
		string[] parts = body.Split(Separator);
		if (parts.Length != TagParts || !Enum.TryParse(parts[0], out RemnantKind kind)
		    || !Enum.TryParse(parts[1], out Rarity rarity))
		{
			return false;
		}

		tag = (kind, rarity);
		return true;
	}
}
