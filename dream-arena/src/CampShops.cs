using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace DreamArena;

/// <summary>
/// Rings the camp rift with free remnants, one per item kind and rarity.
/// </summary>
/// <remarks>
/// <para>
/// A PvP match is only interesting if everyone can actually build what they
/// want, so the camp gets ten remnants in a circle around the rift, with an
/// opening toward the camp to walk in through: memories down one half, essences
/// down the other, each running Common to Unique. Everything is free, since gold
/// is PvE progression. A remnant can be used as often as a player likes; what
/// runs out is the individual item, which disappears from that player's
/// offering once taken. The ledger is per player, so two players wanting the
/// same memory both get it.
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

	/// <summary>Width of the opening in the ring, in degrees — the way in to the rift.</summary>
	private const float OpeningAngle = 90f;

	/// <summary>Ring radii tried, widest first, before giving up.</summary>
	private static readonly float[] RingRadii = { 7f, 6.5f, 6f, 5.5f, 5f };

	/// <summary>
	/// Small turns of the whole ring, tried in order, to dodge a blocked slot
	/// without moving the opening far off the camp.
	/// </summary>
	private static readonly float[] RingRotations = { 0f, 15f, -15f, 30f, -30f, 45f, -45f };

	/// <summary>
	/// How far a slot may sit from walkable ground and still count as placed.
	/// </summary>
	/// <remarks>
	/// Small on purpose. A slot is never nudged to fit — either every slot in the
	/// ring lands on open ground or that candidate is discarded, which is what
	/// keeps the circle even.
	/// </remarks>
	private const float SlotTolerance = 0.65f;

	/// <summary>What each spawned remnant offers, keyed by instance id.</summary>
	private static readonly Dictionary<int, ShopAssignment> Assignments =
		new Dictionary<int, ShopAssignment>();

	/// <summary>Item type names each player has already taken, keyed by guid.</summary>
	/// <remarks>
	/// Server-side and per run, cleared alongside the rest of the arena state when
	/// a game starts. It deliberately outlives a single camp: a run visits the
	/// camp several times, and a limit that reset each visit would just be a
	/// slower way of handing out the same memory over and over.
	/// </remarks>
	private static readonly Dictionary<string, HashSet<string>> Taken =
		new Dictionary<string, HashSet<string>>();

	private static int _builtNode = -1;

	/// <summary>Seconds between build attempts while a camp has no remnants yet.</summary>
	private const float RetryInterval = 1f;

	/// <summary>Attempts per camp before giving up on it.</summary>
	/// <remarks>
	/// A failed build runs the full formation solver — hundreds of ground raycasts
	/// and navmesh samples — and logs a warning. Retrying that every frame, as
	/// this used to, tanked the host's frame rate in any camp too cramped for the
	/// rows, and since the host is the server every client lagged with it.
	/// </remarks>
	private const int MaxAttempts = 5;

	private static int _attemptNode = -1;
	private static int _attempts;
	private static float _nextAttemptTime;

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

	/// <summary>Whether this player has already taken this item.</summary>
	public static bool HasTaken(string playerGuid, string typeName)
	{
		return playerGuid != null
		       && Taken.TryGetValue(playerGuid, out HashSet<string> taken)
		       && taken.Contains(typeName);
	}

	/// <summary>Strikes an item off a player's list for the rest of the run.</summary>
	public static void RecordTaken(string playerGuid, string typeName)
	{
		if (playerGuid == null || string.IsNullOrEmpty(typeName))
		{
			return;
		}

		if (!Taken.TryGetValue(playerGuid, out HashSet<string> taken))
		{
			taken = new HashSet<string>();
			Taken[playerGuid] = taken;
		}

		taken.Add(typeName);
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

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.isInAnyTransition)
		{
			return;
		}

		int node = zones.currentNodeIndex;
		if (node == _builtNode)
		{
			return;
		}

		if (node != _attemptNode)
		{
			_attemptNode = node;
			_attempts = 0;
			_nextAttemptTime = 0f;
		}

		if (_attempts >= MaxAttempts || Time.unscaledTime < _nextAttemptTime)
		{
			return;
		}

		_attempts++;
		_nextAttemptTime = Time.unscaledTime + RetryInterval;

		if (Build())
		{
			_builtNode = node;
			return;
		}

		if (_attempts >= MaxAttempts)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}gave up on camp remnants for node {node} " +
			                 $"after {MaxAttempts} attempts.");
		}
	}

	/// <summary>
	/// The camp's layout: the rift on the ground, and the direction the ring
	/// opens toward, which faces the open camp. False when there is no rift yet.
	/// </summary>
	public static bool TryGetCampLayout(out Vector3 center, out Vector3 openingDir)
	{
		center = default;
		openingDir = Vector3.forward;

		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		if (rift.IsNullOrInactive())
		{
			return false;
		}

		center = Dew.GetPositionOnGround(rift.transform.position);

		if (TryGetCampAnchor(out Vector3 anchor))
		{
			Vector3 toCamp = anchor - center;
			toCamp.y = 0f;
			if (toCamp.sqrMagnitude > 0.01f)
			{
				openingDir = toCamp.normalized;
			}
		}

		return true;
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

		// The ring is built around the rift, so there must be one.
		if (!TryGetCampLayout(out Vector3 center, out Vector3 openingDir))
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no camp rift to ring; skipping camp remnants.");
			return false;
		}

		int rarities = ShopCatalog.StockedRarities.Length;
		int count = rarities * 2;
		if (!TrySolveRing(center, openingDir, count, out Vector3[] slots, out Vector3[] facings))
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}no room for a ring of {count} remnants " +
			                 "around the rift; skipping camp remnants.");
			return false;
		}

		Assignments.Clear();

		// Going around from one edge of the opening: memories Common->Unique down
		// one half, then essences Unique->Common down the other, so the two Commons
		// flank the opening and the Uniques meet at the back.
		int built = 0;
		for (int i = 0; i < count; i++)
		{
			bool isMemory = i < rarities;
			int r = isMemory ? i : count - 1 - i;
			Rarity rarity = ShopCatalog.StockedRarities[r];
			Quaternion rot = Quaternion.LookRotation(facings[i], Vector3.up);

			built += isMemory
				? Place(memoryPrefab, ShopKind.Memory, rarity, slots[i], rot)
				: Place(essencePrefab, ShopKind.Essence, rarity, slots[i], rot);
		}

		Debug.Log($"{ModIdentity.LogPrefix}built {built} camp remnant(s) in a ring around " +
		          $"the rift at {center}.");

		return built > 0;
	}

	/// <summary>
	/// Finds a radius and rotation where every remnant in the ring fits.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The remnants ring the rift with a gap left open toward the camp, so
	/// players walk in through the opening to reach the rift. The circle is
	/// centred on the rift and never moved: the rift is a scene object with no
	/// position syncing, so relocating it host-side would leave clients
	/// interacting with thin air.
	/// </para>
	/// <para>
	/// The layout is rigid. Slot positions come from exact circle maths and are
	/// never nudged sideways; only their height is taken from the ground. A
	/// candidate radius and rotation is accepted only when every slot lands on
	/// walkable ground, so the result is a clean ring or none at all. Radii are
	/// tried widest-first; for each, the whole ring is turned by small amounts
	/// to dodge a blocked slot while keeping the opening near the camp.
	/// </para>
	/// </remarks>
	private static bool TrySolveRing(Vector3 center, Vector3 openingDir, int count,
		out Vector3[] slots, out Vector3[] facings)
	{
		slots = null;
		facings = null;

		// Angle (degrees, measured from +Z) that the opening points along.
		float openingAngle = Mathf.Atan2(openingDir.x, openingDir.z) * Mathf.Rad2Deg;

		foreach (float radius in RingRadii)
		{
			foreach (float rotation in RingRotations)
			{
				if (TryLayOutRing(center, openingAngle + rotation, radius, count,
					    out slots, out facings))
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Lays out one ring: <paramref name="count"/> slots evenly across the arc
	/// left once the opening is removed, or reports failure.
	/// </summary>
	/// <remarks>
	/// Slots run from one edge of the opening, around the back, to the other
	/// edge, so the ring is symmetric about the opening. Each slot faces inward
	/// toward the rift, so a player standing inside the ring sees their fronts.
	/// Only the height of a slot is taken from the navmesh; its ground position
	/// is left exactly on the circle, so the ring stays even.
	/// </remarks>
	private static bool TryLayOutRing(Vector3 center, float openingAngle, float radius,
		int count, out Vector3[] slots, out Vector3[] facings)
	{
		slots = new Vector3[count];
		facings = new Vector3[count];

		float arc = 360f - OpeningAngle;
		float backAngle = openingAngle + 180f;
		float start = backAngle - arc * 0.5f;
		float step = arc / (count - 1);

		for (int i = 0; i < count; i++)
		{
			float a = (start + step * i) * Mathf.Deg2Rad;
			Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
			Vector3 target = center + dir * radius;

			if (!TryGroundSlot(target, out Vector3 slot))
			{
				return false;
			}

			slots[i] = slot;
			facings[i] = -dir;
		}

		return true;
	}
	/// <summary>
	/// Takes a slot's height from the ground, refusing to move it sideways.
	/// </summary>
	internal static bool TryGroundSlot(Vector3 target, out Vector3 slot)
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
	/// <para>
	/// Both use counts stay at the game's own "no limit" value. Neither is the
	/// right lever here: <c>maxTotalUseCount</c> is shared, so one player picking
	/// would use up everybody's turn, and <c>maxUseCountPerPlayer</c> would close
	/// the whole bracket after a single memory rather than just that memory. The
	/// once-each rule is enforced on the offering instead — see
	/// <c>Patches.CampRemnantChoices</c> — so the remnant stays open and only what
	/// a player has already taken goes away.
	/// </para>
	/// <para>
	/// The cooldown is shared across players — it is one last-use timestamp on the
	/// remnant — so it goes too, otherwise one player picking would briefly lock
	/// everybody else out.
	/// </para>
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
		Taken.Clear();
		_builtNode = -1;
		_attemptNode = -1;
		_attempts = 0;
		_nextAttemptTime = 0f;
	}
}
