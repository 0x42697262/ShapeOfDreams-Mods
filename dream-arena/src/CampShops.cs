using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

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
	private const float AisleWidth = 6f;

	/// <summary>Gap between neighbouring remnants in a row, in metres.</summary>
	private const float RowSpacing = 4f;

	/// <summary>How close to the rift a remnant may stand.</summary>
	private const float RiftClearance = 6f;

	/// <summary>
	/// How far a slot may sit from walkable ground and still count as placed.
	/// </summary>
	/// <remarks>
	/// Small on purpose. A slot is never nudged to fit — either the whole
	/// formation lands on open ground or that candidate layout is discarded,
	/// which is what keeps the rows straight.
	/// </remarks>
	private const float SlotTolerance = 0.65f;

	/// <summary>Headings tried when looking for somewhere the rows fit.</summary>
	private const int HeadingCount = 24;

	/// <summary>Spacings tried, largest first, before giving up on a heading.</summary>
	private static readonly float[] SpacingScales = { 1f, 0.85f, 0.7f, 0.55f };

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

		int rows = ShopCatalog.StockedRarities.Length;
		if (!TrySolveFormation(anchor, rows, out Formation formation))
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no room in the camp for " +
			                 $"{rows * 2} aligned remnants; skipping camp shops.");
			return false;
		}

		Assignments.Clear();

		Quaternion faceRight = Quaternion.LookRotation(formation.Sideways, Vector3.up);
		Quaternion faceLeft = Quaternion.LookRotation(-formation.Sideways, Vector3.up);

		int built = 0;
		for (int i = 0; i < rows; i++)
		{
			Rarity rarity = ShopCatalog.StockedRarities[i];

			built += Place(memoryPrefab, ShopKind.Memory, rarity,
				formation.Slots[i * 2], faceRight);

			built += Place(essencePrefab, ShopKind.Essence, rarity,
				formation.Slots[i * 2 + 1], faceLeft);
		}

		Debug.Log($"{ModIdentity.LogPrefix}built {built} camp remnant(s): two rows of " +
		          $"{rows} at {formation.Origin}, heading {formation.Forward}, " +
		          $"spacing {formation.Scale:0.00}x.");

		return built > 0;
	}

	/// <summary>A solved layout: ten slots in two straight, evenly spaced rows.</summary>
	private readonly struct Formation
	{
		public readonly Vector3[] Slots;
		public readonly Vector3 Origin;
		public readonly Vector3 Forward;
		public readonly Vector3 Sideways;
		public readonly float Scale;

		public Formation(Vector3[] slots, Vector3 origin, Vector3 forward, Vector3 sideways,
			float scale)
		{
			Slots = slots;
			Origin = origin;
			Forward = forward;
			Sideways = sideways;
			Scale = scale;
		}
	}

	/// <summary>
	/// Finds a place and heading where both rows fit without bending.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The earlier version placed each remnant with a navmesh sweep that pulled
	/// an unreachable slot back toward the anchor. Every blocked slot therefore
	/// landed somewhere different, which is why the camp ended up looking like
	/// the remnants had been scattered rather than arranged.
	/// </para>
	/// <para>
	/// The layout is rigid instead. Slot positions come from exact grid maths
	/// and are never moved sideways — only their height is taken from the
	/// ground. A candidate origin, heading and spacing is accepted only when
	/// <em>every</em> slot lands on walkable ground, so the result is either a
	/// clean formation or none at all. Candidates are tried widest-first and
	/// scored so that the rows sit in the middle of the room: keeping clear of
	/// the rift earns a candidate points only up to <see cref="RiftClearance"/>,
	/// after which the distance from the anchor decides, so a layout only drifts
	/// off-centre as far as the rift forces it to. The rows move rather than the
	/// rift because the rift is a scene object with no position syncing, so
	/// relocating it host-side would leave clients interacting with thin air.
	/// </para>
	/// </remarks>
	private static bool TrySolveFormation(Vector3 anchor, int rows, out Formation formation)
	{
		formation = default;

		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		bool hasRift = !rift.IsNullOrInactive();
		Vector3 riftPosition = hasRift ? rift.transform.position : Vector3.zero;

		Vector3[] origins = BuildOrigins(anchor, hasRift, riftPosition);

		Vector3[] slots = new Vector3[rows * 2];
		Vector3[] best = null;
		float bestScore = float.NegativeInfinity;
		float bestClearance = 0f;
		Vector3 bestOrigin = anchor;
		Vector3 bestForward = Vector3.forward;
		Vector3 bestSideways = Vector3.right;
		float bestScale = 1f;

		foreach (float scale in SpacingScales)
		{
			foreach (Vector3 origin in origins)
			{
				for (int h = 0; h < HeadingCount; h++)
				{
					float angle = h * (360f / HeadingCount);
					Vector3 forward = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
					Vector3 sideways = Vector3.Cross(Vector3.up, forward).normalized;

					if (!TryLayOutRows(origin, forward, sideways, scale, rows, slots,
						    out float clearance))
					{
						continue;
					}

					float clear = hasRift
						? Mathf.Min(clearance, RiftClearance)
						: RiftClearance;

					float score = clear - Vector3.Distance(origin, anchor) * 0.5f;
					if (score <= bestScore)
					{
						continue;
					}

					bestScore = score;
					bestClearance = clear;
					best = (Vector3[])slots.Clone();
					bestOrigin = origin;
					bestForward = forward;
					bestSideways = sideways;
					bestScale = scale;
				}
			}

			// A tighter spacing is only worth trying if nothing fit at this one.
			if (best != null && (!hasRift || bestClearance >= RiftClearance))
			{
				break;
			}
		}

		if (best == null)
		{
			return false;
		}

		formation = new Formation(best, bestOrigin, bestForward, bestSideways, bestScale);
		return true;
	}

	/// <summary>
	/// Centres to try: the anchor first, then short steps away from the rift.
	/// </summary>
	/// <remarks>
	/// Only an escape hatch for when the rift sits on the anchor. The scoring in
	/// <see cref="TrySolveFormation"/> prefers the anchor, so these outer steps
	/// win only when the anchor cannot clear the rift at any heading or spacing.
	/// </remarks>
	private static Vector3[] BuildOrigins(Vector3 anchor, bool hasRift, Vector3 riftPosition)
	{
		if (!hasRift)
		{
			return new[] { anchor };
		}

		Vector3 away = anchor - riftPosition;
		away.y = 0f;

		if (away.sqrMagnitude < 0.01f)
		{
			return new[] { anchor };
		}

		away.Normalize();

		return new[]
		{
			anchor,
			Dew.GetValidAgentPosition(anchor + away * 3f),
			Dew.GetValidAgentPosition(anchor + away * 6f)
		};
	}

	/// <summary>
	/// Fills <paramref name="slots"/> with one exact grid, or reports failure.
	/// </summary>
	/// <remarks>
	/// Rows are centred on the origin so the formation grows symmetrically
	/// rather than running off in one direction. Only the height of a slot is
	/// taken from the navmesh; its ground position is left exactly where the
	/// grid put it, so the rows stay straight and evenly spaced.
	/// </remarks>
	private static bool TryLayOutRows(Vector3 origin, Vector3 forward, Vector3 sideways,
		float scale, int rows, Vector3[] slots, out float riftClearance)
	{
		riftClearance = float.MaxValue;

		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		bool hasRift = !rift.IsNullOrInactive();
		Vector2 riftFlat = hasRift ? rift.transform.position.ToXY() : Vector2.zero;

		float column = AisleWidth * scale * 0.5f;
		float spacing = RowSpacing * scale;
		float firstRow = -(rows - 1) * 0.5f * spacing;

		for (int i = 0; i < rows; i++)
		{
			Vector3 along = forward * (firstRow + i * spacing);

			for (int side = 0; side < 2; side++)
			{
				Vector3 target = origin + along
				                 + sideways * (side == 0 ? -column : column);

				if (!TryGroundSlot(target, out Vector3 slot))
				{
					return false;
				}

				if (hasRift)
				{
					float distance = Vector2.Distance(slot.ToXY(), riftFlat);
					if (distance < riftClearance)
					{
						riftClearance = distance;
					}
				}

				slots[i * 2 + side] = slot;
			}
		}

		return true;
	}

	/// <summary>
	/// Takes a slot's height from the ground, refusing to move it sideways.
	/// </summary>
	private static bool TryGroundSlot(Vector3 target, out Vector3 slot)
	{
		slot = Dew.GetPositionOnGround(target);

		if (!NavMesh.SamplePosition(slot, out NavMeshHit hit, SlotTolerance, NavMesh.AllAreas))
		{
			return false;
		}

		slot.y = hit.position.y;
		return true;
	}

	private static int Place<T>(T prefab, ShopKind kind, Rarity rarity, Vector3 position,
		Quaternion rotation) where T : ChoiceShrine
	{
		T remnant = Dew.CreateActor(prefab, position, rotation, null, Configure);

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
