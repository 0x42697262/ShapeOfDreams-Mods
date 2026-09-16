using System.Collections.Generic;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Remembers every déjà vu item each player has picked, so they can bring a
/// full build into the arena instead of a single item.
/// </summary>
/// <remarks>
/// <para>
/// A player only has one déjà vu slot: <c>DewPlayer.selectedDejavuItem</c> is a
/// single string SyncVar, and the picker UI has one slot. There is no count to
/// raise — the limit is the data model.
/// </para>
/// <para>
/// The way around it without touching clients: <c>CmdSetDejavuItem</c> is sent
/// from the client every time they change their pick, and it is validated and
/// applied <b>on the host</b>. Recording each accepted pick here builds the
/// player's list on the server side, so they pick items one at a time in the
/// stock UI and the host remembers all of them.
/// </para>
/// <para>
/// Clearing the selection clears the list, which is the "start over" gesture.
/// </para>
/// </remarks>
internal static class LoadoutRegistry
{
	/// <summary>Skill (Memory) resource names start with this.</summary>
	public const string MemoryPrefix = "St_";

	/// <summary>Gem (Essence) resource names start with this.</summary>
	public const string EssencePrefix = "Gem_";

	private static readonly Dictionary<DewPlayer, List<string>> Picks =
		new Dictionary<DewPlayer, List<string>>();

	/// <summary>Records an accepted pick. Empty input clears that player's list.</summary>
	public static void Record(DewPlayer player, string item)
	{
		if (player == null)
		{
			return;
		}

		if (string.IsNullOrEmpty(item))
		{
			if (Picks.Remove(player))
			{
				Debug.Log($"{ModIdentity.LogPrefix}loadout cleared for {player.name}.");
			}

			return;
		}

		if (!Picks.TryGetValue(player, out List<string> list))
		{
			list = new List<string>();
			Picks[player] = list;
		}

		// Re-picking something already banked is a no-op rather than a duplicate.
		if (list.Contains(item))
		{
			return;
		}

		list.Add(item);

		Debug.Log($"{ModIdentity.LogPrefix}{player.name} banked '{item}' " +
		          $"({Describe(list)}).");
	}

	/// <summary>
	/// The player's banked items, trimmed to the configured limits.
	/// </summary>
	/// <remarks>
	/// Earlier picks win when over the limit, so a player's first choices are
	/// the ones that count.
	/// </remarks>
	public static List<string> GetLoadout(DewPlayer player, DreamArenaConfig config)
	{
		List<string> result = new List<string>();

		if (player == null || config == null || !Picks.TryGetValue(player, out List<string> list))
		{
			return result;
		}

		int essences = 0;
		int memories = 0;

		foreach (string item in list)
		{
			if (IsMemory(item))
			{
				if (memories >= config.maxMemories)
				{
					continue;
				}

				memories++;
			}
			else if (IsEssence(item))
			{
				if (essences >= config.maxEssences)
				{
					continue;
				}

				essences++;
			}
			else
			{
				continue;
			}

			result.Add(item);
		}

		return result;
	}

	public static bool IsMemory(string item)
	{
		return item != null && item.StartsWith(MemoryPrefix, System.StringComparison.Ordinal);
	}

	public static bool IsEssence(string item)
	{
		return item != null && item.StartsWith(EssencePrefix, System.StringComparison.Ordinal);
	}

	private static string Describe(List<string> list)
	{
		int memories = 0;
		int essences = 0;

		foreach (string item in list)
		{
			if (IsMemory(item))
			{
				memories++;
			}
			else if (IsEssence(item))
			{
				essences++;
			}
		}

		return $"{memories} memories, {essences} essences";
	}

	public static void Clear(DewPlayer player)
	{
		if (player != null)
		{
			Picks.Remove(player);
		}
	}

	public static void ClearAll()
	{
		Picks.Clear();
	}

	public static int Count => Picks.Count;
}
