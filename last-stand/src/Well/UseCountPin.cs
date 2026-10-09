using System.Reflection;
using HarmonyLib;
using LastStand.Game;
using Mirror;

namespace LastStand.Well;

internal static class UseCountPin
{
	private static readonly FieldInfo UseCounts = AccessTools.Field(typeof(Shrine), GameNames.ShrineUseCounts);

	public static void Set(Shrine shrine, DewPlayer player, int uses)
	{
		if (!shrine.IsNullOrInactive() && player != null && player.guid != null
		    && UseCounts?.GetValue(shrine) is SyncDictionary<string, int> counts)
		{
			counts[player.guid] = uses;
		}
	}

	public static void Pin(Shrine shrine, int uses)
	{
		if (shrine.IsNullOrInactive() || DewPlayer.gamePlayers == null
		    || !(UseCounts?.GetValue(shrine) is SyncDictionary<string, int> counts))
		{
			return;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null && player.guid != null && shrine.GetUseCountByPlayer(player) != uses)
			{
				counts[player.guid] = uses;
			}
		}
	}
}
