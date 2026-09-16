using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Stands up a row of free shops in the camp, one per item kind and rarity.
/// </summary>
/// <remarks>
/// <para>
/// A PvP match is only interesting if everyone can actually build what they
/// want, so the camp becomes a shopping floor: Memories along one row,
/// Essences along another, each split by rarity. Everything is free, since
/// gold is a PvE progression currency and has no meaning here.
/// </para>
/// <para>
/// Merchants are ordinary <c>PropEnt_Merchant_Jonas</c> entities, spawned
/// server-side like any other prop, so unmodded clients see and use them
/// normally. Their stock is replaced wholesale — see
/// <c>Patches.CampShopStockPatch</c>.
/// </para>
/// </remarks>
internal static class CampShops
{
	private const string MerchantResource = "PropEnt_Merchant_Jonas";

	/// <summary>Spacing between shops along a row, in metres.</summary>
	private const float ColumnSpacing = 4.5f;

	/// <summary>Gap between the Memories row and the Essences row.</summary>
	private const float RowSpacing = 5f;

	/// <summary>Which shop each spawned merchant is, keyed by instance id.</summary>
	private static readonly Dictionary<int, ShopAssignment> Assignments =
		new Dictionary<int, ShopAssignment>();

	private static int _builtNode = -1;

	internal readonly struct ShopAssignment
	{
		public readonly ShopKind Kind;
		public readonly Rarity Rarity;

		public ShopAssignment(ShopKind kind, Rarity rarity)
		{
			Kind = kind;
			Rarity = rarity;
		}
	}

	/// <summary>Looks up what a merchant is meant to sell, if it is one of ours.</summary>
	public static bool TryGetAssignment(Component merchant, out ShopAssignment assignment)
	{
		assignment = default;

		return merchant != null
		       && Assignments.TryGetValue(merchant.GetInstanceID(), out assignment);
	}

	public static void Tick(DreamArenaConfig config, PhaseKind phase)
	{
		if (!NetworkServer.active || config == null
		    || !config.arenaEnabled || !config.campShops)
		{
			return;
		}

		if (phase != PhaseKind.Camp)
		{
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.instance;
		if (zones == null || zones.isInAnyTransition)
		{
			return;
		}

		if (zones.currentNodeIndex == _builtNode)
		{
			return;
		}

		if (Build(config))
		{
			_builtNode = zones.currentNodeIndex;
		}
	}

	private static bool Build(DreamArenaConfig config)
	{
		PropEnt_Merchant_Jonas prefab =
			DewResources.GetByShortTypeName<PropEnt_Merchant_Jonas>(MerchantResource);

		if (prefab == null)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}merchant prefab '{MerchantResource}' not found; " +
			                 "no camp shops.");
			return false;
		}

		if (!TryGetCampAnchor(out Vector3 anchor))
		{
			return false;
		}

		Assignments.Clear();

		int columns = ShopCatalog.StockedRarities.Length;
		float rowWidth = ColumnSpacing * (columns - 1);

		int built = 0;
		built += BuildRow(prefab, ShopKind.Memory, anchor, -RowSpacing * 0.5f, rowWidth);
		built += BuildRow(prefab, ShopKind.Essence, anchor, RowSpacing * 0.5f, rowWidth);

		Debug.Log($"{ModIdentity.LogPrefix}built {built} camp shop(s) around {anchor}.");
		return built > 0;
	}

	private static int BuildRow(PropEnt_Merchant_Jonas prefab, ShopKind kind, Vector3 anchor,
		float rowOffset, float rowWidth)
	{
		int built = 0;

		for (int i = 0; i < ShopCatalog.StockedRarities.Length; i++)
		{
			Rarity rarity = ShopCatalog.StockedRarities[i];

			Vector3 local = new Vector3(i * ColumnSpacing - rowWidth * 0.5f, 0f, rowOffset);
			Vector3 target = Dew.GetValidAgentDestination_LinearSweep(anchor, anchor + local);

			PropEnt_Merchant_Jonas merchant = Dew.SpawnEntity(
				prefab, target, Quaternion.identity, null, DewPlayer.environment, 1);

			if (merchant == null)
			{
				continue;
			}

			merchant.name = ShopCatalog.Describe(kind, rarity) + ModIdentity.OwnedSuffix;
			Assignments[merchant.GetInstanceID()] = new ShopAssignment(kind, rarity);
			built++;
		}

		return built;
	}

	/// <summary>
	/// Where to put the shopping floor.
	/// </summary>
	/// <remarks>
	/// The camp's largest section is its open area, and its <c>pathablePivot</c>
	/// is a centre already known to be walkable — the same reasoning the arena
	/// placement uses.
	/// </remarks>
	private static bool TryGetCampAnchor(out Vector3 anchor)
	{
		anchor = Vector3.zero;

		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || room.sections == null || room.sections.Count == 0)
		{
			return false;
		}

		RoomSection biggest = null;
		foreach (RoomSection section in room.sections)
		{
			if (section != null && (biggest == null || section.area > biggest.area))
			{
				biggest = section;
			}
		}

		if (biggest == null)
		{
			return false;
		}

		anchor = Dew.GetValidAgentPosition(biggest.pathablePivot);
		return true;
	}

	public static void Reset()
	{
		Assignments.Clear();
		_builtNode = -1;
	}
}
