using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Stands up two rows of free remnants in the camp, one per item kind and rarity.
/// </summary>
/// <remarks>
/// <para>
/// A PvP match is only interesting if everyone can actually build what they
/// want, so the camp gets ten remnants: memories down the left, essences down
/// the right, each row running Common to Unique. Everything is free and
/// unlimited, since gold is PvE progression and two players wanting the same
/// build is normal.
/// </para>
/// <para>
/// These are <c>ChoiceShrine</c>s — the Retrospection and Enlightenment
/// remnants — rather than merchants. A remnant opens a pick-one window whose
/// contents are per player and server-authored, which is exactly the shape
/// this needs; a merchant's shop window is a purchase flow built around gold
/// and stock counts that mean nothing here.
/// </para>
/// <para>
/// They are ordinary game actors spawned server-side, so unmodded clients see
/// and use them normally. Their offering is replaced wholesale — see
/// <c>Patches.CampRemnantChoicesPatch</c>.
/// </para>
/// </remarks>
internal static class CampShops
{
	private const string MemoryRemnant = "Shrine_Retrospection";
	private const string EssenceRemnant = "Shrine_Enlightenment";

	/// <summary>Width of the aisle between the two rows, in metres.</summary>
	private const float AisleWidth = 7f;

	/// <summary>Gap between neighbouring remnants in a row, in metres.</summary>
	private const float RowSpacing = 4.5f;

	/// <summary>How far clear of the rift the first pair stands.</summary>
	private const float RiftClearance = 8f;

	/// <summary>What each spawned remnant offers, keyed by instance id.</summary>
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

	/// <summary>Looks up what a remnant offers, if it is one of ours.</summary>
	public static bool TryGetAssignment(Component remnant, out ShopAssignment assignment)
	{
		assignment = default;

		return remnant != null
		       && Assignments.TryGetValue(remnant.GetInstanceID(), out assignment);
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

		if (Build())
		{
			_builtNode = zones.currentNodeIndex;
		}
	}

	private static bool Build()
	{
		Shrine_Retrospection memoryPrefab =
			DewResources.GetByShortTypeName<Shrine_Retrospection>(MemoryRemnant);

		Shrine_Enlightenment essencePrefab =
			DewResources.GetByShortTypeName<Shrine_Enlightenment>(EssenceRemnant);

		if (memoryPrefab == null || essencePrefab == null)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}remnant prefabs not found " +
			                 $"('{MemoryRemnant}', '{EssenceRemnant}'); no camp remnants.");
			return false;
		}

		if (!TryGetCampAnchor(out Vector3 anchor))
		{
			return false;
		}

		Assignments.Clear();

		GetRowAxes(anchor, out Vector3 origin, out Vector3 forward, out Vector3 sideways);

		Vector3 leftColumn = sideways * (-AisleWidth * 0.5f);
		Vector3 rightColumn = sideways * (AisleWidth * 0.5f);

		int built = 0;
		for (int i = 0; i < ShopCatalog.StockedRarities.Length; i++)
		{
			Rarity rarity = ShopCatalog.StockedRarities[i];
			Vector3 along = forward * (i * RowSpacing);

			built += Place(memoryPrefab, ShopKind.Memory, rarity, origin, along + leftColumn);
			built += Place(essencePrefab, ShopKind.Essence, rarity, origin, along + rightColumn);
		}

		Debug.Log($"{ModIdentity.LogPrefix}built {built} camp remnant(s) around {origin}.");
		return built > 0;
	}

	/// <summary>
	/// Works out where the two rows run so they never block the rift.
	/// </summary>
	/// <remarks>
	/// The rift is a scene object with no position syncing, so moving it would
	/// shift it on the host only and leave clients interacting with thin air.
	/// The remnants are laid out around it instead: the rows run directly away
	/// from the rift, straddling an aisle, and start a clear distance from it.
	/// That keeps the rift approachable and adapts to whatever camp room the
	/// run generates rather than assuming a fixed direction.
	/// </remarks>
	private static void GetRowAxes(Vector3 anchor, out Vector3 origin, out Vector3 forward,
		out Vector3 sideways)
	{
		origin = anchor;
		forward = Vector3.forward;
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

		forward = away.normalized;
		sideways = Vector3.Cross(Vector3.up, forward).normalized;

		origin = Dew.GetValidAgentDestination_LinearSweep(anchor, anchor + forward * RiftClearance);
	}

	private static int Place<T>(T prefab, ShopKind kind, Rarity rarity, Vector3 origin,
		Vector3 offset) where T : ChoiceShrine
	{
		Vector3 target = Dew.GetValidAgentDestination_LinearSweep(origin, origin + offset);

		T remnant = Dew.CreateActor(prefab, target, Quaternion.identity, null, Configure);

		if (remnant == null)
		{
			return 0;
		}

		remnant.name = ShopCatalog.Describe(kind, rarity) + ModIdentity.OwnedSuffix;
		Assignments[remnant.GetInstanceID()] = new ShopAssignment(kind, rarity);
		return 1;
	}

	/// <summary>
	/// Makes a remnant a permanent fixture rather than a one-off reward.
	/// </summary>
	/// <remarks>
	/// A negative use count is the game's own "no limit" value, checked as
	/// <c>maxTotalUseCount &lt; 0</c> before every use, so these never run dry
	/// for anyone. The cooldown is shared across players, so it goes too —
	/// otherwise one player picking would briefly lock everybody else out.
	/// </remarks>
	private static void Configure(ChoiceShrine remnant)
	{
		remnant.maxTotalUseCount = -1;
		remnant.maxUseCountPerPlayer = -1;
		remnant.useCooldown = 0f;
		remnant.baseGoldCost = 0;
		remnant.unavailableByDefault = false;
		remnant.interactableDelay = 0f;
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
