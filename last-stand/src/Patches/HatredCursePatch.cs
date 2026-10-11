using HarmonyLib;
using LastStand.Game;
using Mirror;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Shrine_Hatred), GameNames.HatredDoCurse)]
internal static class HatredCursePatch
{
	[HarmonyPostfix]
	private static void Postfix(HatredStrengthType strength, Hero hero)
	{
		if (NetworkServer.active && ModMode.IsHosting())
		{
			LastStandMod.Current?.Session?.OnHatredCurse(hero, strength);
		}
	}
}
