using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Turns PvP on and off by writing the players' team-relation lists.
/// </summary>
/// <remarks>
/// <para>
/// <c>DewPlayer</c> carries three <c>SyncList&lt;DewPlayer&gt;</c> fields:
/// <c>allies</c>, <c>enemies</c> and <c>neutrals</c>. Vanilla never adds to
/// any of them; the system is fully built and simply dormant. (The leftover
/// private helpers <c>ClearRelationship</c> and <c>ResolveTwoPlayersAndLog</c>
/// in <c>DewConsoleCommands</c> are unused.)
/// </para>
/// <para>
/// <c>GetTeamRelation</c> checks <c>enemies</c> well before its final
/// "both are human players, so Ally" default, so an explicit entry wins.
/// Every damaging ability validates targets through
/// <c>AbilityTargetValidator</c>, which defaults to
/// <c>Neutral | Enemy</c> — so a hostile player becomes a legal target with
/// no other change. There is no separate friendly-fire flag anywhere.
/// </para>
/// <para>
/// These are SyncLists, so writing them on the server replicates to every
/// client. Clients need no mod: their own <c>GetTeamRelation</c> reads the
/// synced list, and <c>TeamRelationChanged</c> is already wired to refresh
/// healthbars, so nameplates recolor on their side automatically.
/// </para>
/// <para>
/// Relations are <b>directional</b>: <c>a.GetTeamRelation(b)</c> only reads
/// a's lists. Every change here is applied to both sides.
/// </para>
/// </remarks>
internal static class RelationController
{
	public static void Apply(TeamMode mode)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}relation change ignored: not the server.");
			return;
		}

		List<DewPlayer> players = GetHumanPlayers();
		ClearAll(players);

		if (mode == TeamMode.Peaceful)
		{
			Debug.Log($"{ModIdentity.LogPrefix}relations cleared; players are friendly again.");
			return;
		}

		int hostile = 0;

		for (int i = 0; i < players.Count; i++)
		{
			for (int j = i + 1; j < players.Count; j++)
			{
				DewPlayer a = players[i];
				DewPlayer b = players[j];

				bool enemies = mode == TeamMode.FreeForAll || TeamOf(i) != TeamOf(j);

				if (enemies)
				{
					MakeEnemies(a, b);
					hostile++;
				}
				else
				{
					MakeAllies(a, b);
				}
			}
		}

		Debug.Log($"{ModIdentity.LogPrefix}{mode} applied to {players.Count} players " +
		          $"({hostile} hostile pairs).");
	}

	/// <summary>Team assignment for <see cref="TeamMode.TwoTeams"/>: alternating by join order.</summary>
	private static int TeamOf(int index)
	{
		return index % 2;
	}

	private static void MakeEnemies(DewPlayer a, DewPlayer b)
	{
		AddUnique(a.enemies, b);
		AddUnique(b.enemies, a);
	}

	private static void MakeAllies(DewPlayer a, DewPlayer b)
	{
		AddUnique(a.allies, b);
		AddUnique(b.allies, a);
	}

	private static void AddUnique(SyncList<DewPlayer> list, DewPlayer player)
	{
		if (player != null && !list.Contains(player))
		{
			list.Add(player);
		}
	}

	/// <summary>
	/// Wipes every relation entry between the given players, in both
	/// directions, so the vanilla defaults apply again.
	/// </summary>
	public static void ClearAll(List<DewPlayer> players)
	{
		if (!NetworkServer.active)
		{
			return;
		}

		foreach (DewPlayer a in players)
		{
			foreach (DewPlayer b in players)
			{
				if (a == null || b == null || a == b)
				{
					continue;
				}

				a.enemies.Remove(b);
				a.allies.Remove(b);
				a.neutrals.Remove(b);
			}
		}
	}

	public static void ClearAll()
	{
		ClearAll(GetHumanPlayers());
	}

	public static List<DewPlayer> GetHumanPlayers()
	{
		List<DewPlayer> result = new List<DewPlayer>();

		foreach (DewPlayer player in DewPlayer.allHumanPlayers)
		{
			if (player != null)
			{
				result.Add(player);
			}
		}

		return result;
	}
}
