using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Text;
using Mirror;

namespace DreamArena.Pvp;

internal static class Relations
{
	private static readonly List<DewPlayer> Players = new List<DewPlayer>();

	public static void Apply(TeamMode mode)
	{
		FillHumanPlayers();
		Clear(Players);
		int hostile = 0;
		for (int i = 0; i < Players.Count; i++)
		{
			for (int j = i + 1; j < Players.Count; j++)
			{
				if (TeamRule.AreEnemies(mode, i, j))
				{
					Pair(Players[i].enemies, Players[j].enemies, Players[i], Players[j]);
					hostile++;
				}
				else if (mode != TeamMode.Peaceful)
				{
					Pair(Players[i].allies, Players[j].allies, Players[i], Players[j]);
				}
			}
		}

		Log.Info(LogText.RelationsApplied(mode, Players.Count, hostile));
		Players.Clear();
	}

	public static void ClearAll()
	{
		FillHumanPlayers();
		Clear(Players);
		Players.Clear();
	}

	private static void Pair(SyncList<DewPlayer> firstList, SyncList<DewPlayer> secondList, DewPlayer first,
		DewPlayer second)
	{
		AddUnique(firstList, second);
		AddUnique(secondList, first);
	}

	private static void AddUnique(SyncList<DewPlayer> list, DewPlayer player)
	{
		if (player != null && !list.Contains(player))
		{
			list.Add(player);
		}
	}

	private static void Clear(List<DewPlayer> players)
	{
		foreach (DewPlayer a in players)
		{
			foreach (DewPlayer b in players)
			{
				if (a == b)
				{
					continue;
				}

				a.enemies.Remove(b);
				a.allies.Remove(b);
				a.neutrals.Remove(b);
			}
		}
	}

	private static void FillHumanPlayers()
	{
		Players.Clear();
		foreach (DewPlayer player in DewPlayer.allHumanPlayers)
		{
			if (player != null)
			{
				Players.Add(player);
			}
		}
	}
}
