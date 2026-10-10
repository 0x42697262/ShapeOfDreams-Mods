using HarmonyLib;
using DreamArena.Camp;
using DreamArena.Game;
using DreamArena.Mode;
using Mirror;

namespace DreamArena.Patches;

internal static class RemnantHooks
{
	public static void Offer(ChoiceShrine remnant, DewPlayer player)
	{
		Session.DreamArenaSession session = DreamArenaMod.Current?.Session;
		if (ArenaMode.IsHosting() && session?.Settings != null)
		{
			RemnantChoices.Offer(remnant, player, session.Remnants, session.Settings.RemnantMemoryLevel,
				session.Settings.RemnantEssenceQuality);
		}
	}
}

[HarmonyPatch(typeof(Shrine_Retrospection), GameNames.PopulateChoices)]
internal static class MemoryRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Retrospection __instance, DewPlayer player)
	{
		RemnantHooks.Offer(__instance, player);
	}
}

[HarmonyPatch(typeof(Shrine_Enlightenment), GameNames.PopulateChoices)]
internal static class EssenceRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Enlightenment __instance, DewPlayer player)
	{
		RemnantHooks.Offer(__instance, player);
	}
}

[HarmonyPatch(typeof(ChoiceShrine), GameNames.ChooseCommand)]
internal static class RemnantChoosePatch
{
	internal struct Pending
	{
		public DewPlayer Player;
		public string Item;
		public int UsesBefore;
	}

	[HarmonyPrefix]
	private static void Prefix(ChoiceShrine __instance, int index, NetworkConnectionToClient sender,
		out Pending __state)
	{
		__state = default;
		DewPlayer player = sender?.GetPlayer();
		if (ArenaMode.IsHosting() && RemnantChoices.TryReadChoice(__instance, index, player, out string item))
		{
			__state = new Pending { Player = player, Item = item, UsesBefore = __instance.GetUseCountByPlayer(player) };
		}
	}

	[HarmonyPostfix]
	private static void Postfix(ChoiceShrine __instance, Pending __state)
	{
		CampRemnants remnants = DreamArenaMod.Current?.Session?.Remnants;
		if (__state.Player != null && remnants != null
		    && __instance.GetUseCountByPlayer(__state.Player) > __state.UsesBefore)
		{
			RemnantChoices.AfterChoice(__instance, __state.Player, __state.Item, remnants);
		}
	}
}

[HarmonyPatch(typeof(Shrine), nameof(Shrine.CanInteract))]
internal static class EmptyRemnantPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine __instance, Entity entity, ref bool __result)
	{
		if (__result && __instance is ChoiceShrine remnant && NetworkServer.active
		    && RemnantChoices.IsEmptyFor(remnant, entity))
		{
			__result = false;
		}
	}
}
