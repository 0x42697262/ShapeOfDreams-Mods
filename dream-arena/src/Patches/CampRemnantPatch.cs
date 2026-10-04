using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Replaces a camp remnant's three random picks with its whole rarity bracket.
/// </summary>
/// <remarks>
/// <para>
/// A remnant normally rolls a rarity, then offers a few random items of it.
/// These offer every memory or essence of their own bracket instead, at a
/// fixed level, so a specific loadout is a matter of choosing rather than
/// luck — and since the remnants never run dry, everyone can take the same
/// thing.
/// </para>
/// <para>
/// One copy each, though. Anything a player has already taken is left out of
/// the offering the next time it is built, which is the whole of the
/// once-each rule: the remnant itself stays open, because what is exhausted is
/// an item and not the remnant. <c>CampRemnantPurchasePatch</c> is what records
/// a pick and rebuilds the offering so the item disappears immediately.
/// </para>
/// <para>
/// The offering is written after the original runs rather than instead of it:
/// the base class does bookkeeping around population, and assigning
/// <c>choices[player.guid]</c> as the last step is exactly what the vanilla
/// implementations do.
/// </para>
/// <para>
/// Remnants that are not ours are left completely alone, so the ones the world
/// generates still behave normally.
/// </para>
/// </remarks>
internal static class CampRemnantChoices
{
	/// <summary>
	/// Offers every item of this remnant's bracket, or does nothing if the
	/// remnant belongs to the game rather than to us.
	/// </summary>
	public static void Apply(ChoiceShrine remnant, DewPlayer player)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled || remnant == null || player == null)
		{
			return;
		}

		if (!CampShops.TryGetAssignment(remnant, out CampShops.ShopAssignment shop))
		{
			return;
		}

		List<string> items = ShopCatalog.GetItems(shop.Kind, shop.Rarity);
		if (items.Count == 0)
		{
			return;
		}

		// Memories read this as a skill level, essences as a gem quality.
		int level = shop.Kind == ShopKind.Memory
			? Mathf.Max(1, mod.config.shopMemoryLevel)
			: Mathf.Max(1, mod.config.shopEssenceQuality);

		List<ChoiceShrineItem> offering = new List<ChoiceShrineItem>(items.Count);
		foreach (string item in items)
		{
			if (CampShops.HasTaken(player.guid, item))
			{
				continue;
			}

			offering.Add(new ChoiceShrineItem
			{
				typeName = item,
				level = level,
			});
		}

		// itemCount is left at the prefab default on purpose. It is only the number
		// of items the vanilla roll produces before this postfix throws them away,
		// and the window sizes its grid from the array below — it reads itemCount
		// for Shrine_Paradox alone. Raising it to the bracket size, as this used to,
		// just made the discarded roll bigger on every repopulate.
		remnant.choices[player.guid] = offering.ToArray();
	}
}

/// <summary>Memory remnants offer a whole memory rarity.</summary>
[HarmonyPatch(typeof(Shrine_Retrospection), "OnPopulateChoices")]
internal static class CampMemoryRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Retrospection __instance, DewPlayer player)
	{
		CampRemnantChoices.Apply(__instance, player);
	}
}

/// <summary>Essence remnants offer a whole essence rarity.</summary>
[HarmonyPatch(typeof(Shrine_Enlightenment), "OnPopulateChoices")]
internal static class CampEssenceRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Enlightenment __instance, DewPlayer player)
	{
		CampRemnantChoices.Apply(__instance, player);
	}
}

/// <summary>
/// Strikes an item off the player who took it, and refreshes their offering.
/// </summary>
/// <remarks>
/// <para>
/// This patches the woven server-side body of <c>CmdChoose</c> rather than
/// <c>CmdChoose</c> itself. Mirror replaces the original method with a stub
/// that sends the command and moves the real work into
/// <c>UserCode_CmdChoose__*</c>; a remote client's pick therefore never runs
/// the stub on the host, so patching the nice name would only ever catch the
/// host's own picks.
/// </para>
/// <para>
/// Success is measured rather than assumed. The command drops the pick if the
/// player cannot afford or interact with the remnant, and says so only by
/// returning early, so the prefix notes the player's use count and the postfix
/// records the item only if that count went up. The count is incremented by
/// <c>DoPostUseRoutines</c>, which the command's coroutine reaches before its
/// first yield and therefore before the postfix runs.
/// </para>
/// <para>
/// Repopulating afterwards is what makes the item vanish from the open window
/// rather than at the next camp: it rebuilds <c>choices[guid]</c> through
/// <c>CampRemnantChoices.Apply</c>, which now filters the item out. The pick
/// already in flight is unaffected — the coroutine captured its own array
/// before this runs.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(ChoiceShrine), "UserCode_CmdChoose__Int32__NetworkConnectionToClient")]
internal static class CampRemnantPurchasePatch
{
	/// <summary>A pick that was accepted for dispatch, pending confirmation.</summary>
	internal struct Pending
	{
		public DewPlayer Player;
		public string TypeName;
		public int UsesBefore;
	}

	[HarmonyPrefix]
	private static void Prefix(ChoiceShrine __instance, int index,
		NetworkConnectionToClient sender, out Pending __state)
	{
		__state = default;

		if (__instance == null || sender == null
		    || !CampShops.TryGetAssignment(__instance, out _))
		{
			return;
		}

		DewPlayer player = sender.GetPlayer();
		if (player == null
		    || !__instance.choices.TryGetValue(player.guid, out ChoiceShrineItem[] items)
		    || index < 0 || index >= items.Length)
		{
			return;
		}

		__state = new Pending
		{
			Player = player,
			TypeName = items[index].typeName,
			UsesBefore = __instance.GetUseCountByPlayer(player),
		};
	}

	[HarmonyPostfix]
	private static void Postfix(ChoiceShrine __instance, Pending __state)
	{
		if (__state.Player == null || string.IsNullOrEmpty(__state.TypeName))
		{
			return;
		}

		if (__instance.GetUseCountByPlayer(__state.Player) <= __state.UsesBefore)
		{
			// The command turned the pick down; nothing was handed over.
			return;
		}

		CampShops.RecordTaken(__state.Player.guid, __state.TypeName);
		__instance.PopulatePlayerChoices(__state.Player);

		// Roll the use count and cooldown back to the untouched state. Both decide
		// whether a player may interact again, and a client judges that with its
		// own copy of the limits. `maxUseCountPerPlayer` and `useCooldown` are not
		// SyncVars, so our spawn-time overrides (-1 and 0) never reach a client; it
		// keeps the prefab's one-pick-per-player rule. The use count and last-use
		// time *are* synced, so clearing them here is what the client sees, and its
		// check (count 0 < limit, no cooldown) passes, letting it pick again. The
		// host is unaffected: it already has no limit. Our own once-each-item rule
		// still holds; it lives in the offering, not the use count.
		__instance.ResetUseCounts();
		__instance.Network_lastUseTime = double.NegativeInfinity;
	}
}

/// <summary>
/// Closes a remnant to a player who has taken everything it had.
/// </summary>
/// <remarks>
/// Without this the remnant stays invitingly lit and opens an empty window.
/// The test is emptiness alone rather than ownership, because it holds for any
/// choice shrine and, unlike our assignment table, an empty
/// <c>choices[guid]</c> is synced and so reads the same on a client — which is
/// where the interaction prompt is actually drawn. A remnant that has not been
/// populated for this player yet has no entry at all, so it is left alone.
/// </remarks>
[HarmonyPatch(typeof(Shrine), nameof(Shrine.CanInteract))]
internal static class EmptyRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine __instance, Entity entity, ref bool __result)
	{
		if (!__result || !(__instance is ChoiceShrine remnant)
		    || entity == null || entity.owner == null)
		{
			return;
		}

		if (remnant.choices.TryGetValue(entity.owner.guid, out ChoiceShrineItem[] items)
		    && items.Length == 0)
		{
			__result = false;
		}
	}
}
