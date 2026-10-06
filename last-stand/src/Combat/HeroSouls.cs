using System;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Combat;

internal static class HeroSouls
{
	public static bool PlaceInCurrentRoom(Se_HeroKnockedOut knockedOut)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		DewPlayer owner = knockedOut != null && knockedOut.victim != null ? knockedOut.victim.owner : null;
		if (zones == null || owner == null || knockedOut.disableQuest)
		{
			return false;
		}

		int current = zones.currentNodeIndex;
		bool alreadyHere = false;
		for (int node = 0; node < zones.nodes.Count; node++)
		{
			foreach (ModifierData modifier in zones.nodes[node].modifiers.ToArray())
			{
				if (!IsSoulOf(modifier, owner))
				{
					continue;
				}

				if (node == current)
				{
					alreadyHere = true;
				}
				else
				{
					zones.RemoveModifier(node, modifier.id);
					Log.Info(LogText.HeroSoulMoved(node, current));
				}
			}
		}

		if (!alreadyHere && Dew.GetAliveHeroCount() != 0)
		{
			zones.AddModifier<RoomMod_HeroSoul>(current, owner.guid);
			Log.Info(LogText.HeroSoulPlaced(current));
		}

		return true;
	}

	private static bool IsSoulOf(ModifierData modifier, DewPlayer owner)
	{
		return string.Equals(modifier.type, GameNames.HeroSoulModifier, StringComparison.Ordinal)
		       && string.Equals(modifier.clientData, owner.guid, StringComparison.Ordinal);
	}
}
