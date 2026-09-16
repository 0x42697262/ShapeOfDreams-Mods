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
/// want, so the camp gets two stalls: one for Memories, one for Essences, each
/// carrying every rarity. Everything is free and effectively unlimited, since
/// gold is PvE progression and two players wanting the same build is normal.
/// </para>
/// <para>
/// This was ten stalls split by rarity, which read more clearly but cost ten
/// merchant entities and their networking in a room players sit in between
/// rounds. Two is enough.
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

	/// <summary>Gap between the two stalls, in metres.</summary>
	private const float StallSpacing = 6f;

	/// <summary>How far clear of the rift the stalls are placed.</summary>
	private const float RiftClearance = 9f;

	/// <summary>Which shop each spawned merchant is, keyed by instance id.</summary>
	private static readonly Dictionary<int, ShopAssignment> Assignments =
		new Dictionary<int, ShopAssignment>();

	private static int _builtNode = -1;

	internal readonly struct ShopAssignment
	{
		public readonly ShopKind Kind;

		public ShopAssignment(ShopKind kind)
		{
			Kind = kind;
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

		GetStallAxes(anchor, out Vector3 origin, out Vector3 sideways);

		int built = 0;
		built += Place(prefab, ShopKind.Memory, origin, sideways * (-StallSpacing * 0.5f));
		built += Place(prefab, ShopKind.Essence, origin, sideways * (StallSpacing * 0.5f));

		Debug.Log($"{ModIdentity.LogPrefix}built {built} camp shop(s) around {origin}.");
		return built > 0;
	}

	/// <summary>
	/// Works out where the stalls stand so they never block the rift.
	/// </summary>
	/// <remarks>
	/// The rift is a scene object with no position syncing, so moving it would
	/// shift it on the host only and leave clients interacting with thin air.
	/// The stalls are moved instead: they are pushed directly away from the
	/// rift and laid out along the perpendicular, which keeps the rift clear
	/// without touching it.
	/// </remarks>
	private static void GetStallAxes(Vector3 anchor, out Vector3 origin, out Vector3 sideways)
	{
		origin = anchor;
		sideways = Vector3.right;

		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		if (rift.IsNullOrInactive())
		{
			return;
		}

		Vector3 away = anchor - rift.transform.position;
		away.y = 0f;

		// Anchor sitting on top of the rift gives no usable direction.
		if (away.sqrMagnitude < 0.01f)
		{
			away = Vector3.forward;
		}

		away = away.normalized;
		sideways = Vector3.Cross(Vector3.up, away).normalized;

		origin = Dew.GetValidAgentDestination_LinearSweep(anchor, anchor + away * RiftClearance);

		Debug.Log($"{ModIdentity.LogPrefix}stalls placed {RiftClearance:0.#}m clear of the rift.");
	}

	private static int Place(PropEnt_Merchant_Jonas prefab, ShopKind kind, Vector3 origin,
		Vector3 offset)
	{
		Vector3 target = Dew.GetValidAgentDestination_LinearSweep(origin, origin + offset);

		PropEnt_Merchant_Jonas merchant = Dew.SpawnEntity(
			prefab, target, Quaternion.identity, null, DewPlayer.environment, 1);

		if (merchant == null)
		{
			return 0;
		}

		merchant.name = ShopCatalog.Describe(kind) + ModIdentity.OwnedSuffix;
		Assignments[merchant.GetInstanceID()] = new ShopAssignment(kind);
		return 1;
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
