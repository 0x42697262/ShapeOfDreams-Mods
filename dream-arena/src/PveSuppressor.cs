using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Strips the PvE layer out of the arena.
/// </summary>
/// <remarks>
/// <para>
/// Zeroing the monster population budget stops waves, but it does not touch
/// everything: room props are spawned by <c>RoomProps.OnRoomStartServer</c> on
/// a separate path, which is where wells, gold and dream-dust stones,
/// merchants and shrines come from. A boss room also places its boss and the
/// hazards it owns as part of the room itself, so those keep appearing even
/// with the population at zero.
/// </para>
/// <para>
/// Prop spawning is blocked at the source; anything that still makes it into
/// the arena is swept up here. Heroes, their summons and rifts are left alone.
/// </para>
/// </remarks>
internal static class PveSuppressor
{
	private const float SweepInterval = 1f;

	private static float _timer;

	private static readonly List<Entity> DoomedEntities = new List<Entity>();
	private static readonly List<Actor> DoomedActors = new List<Actor>();

	/// <summary>
	/// True when PvE content should be kept out of the room the players are in.
	/// </summary>
	/// <remarks>
	/// The camp is left completely alone — it should still look and behave like
	/// a normal starting room.
	/// </remarks>
	public static bool ShouldSuppress(DreamArenaConfig config, PhaseKind phase)
	{
		return config != null && config.arenaEnabled && config.suppressMonsters
		       && phase == PhaseKind.Arena;
	}

	public static void Tick(float deltaTime, DreamArenaConfig config, PhaseKind phase)
	{
		if (!NetworkServer.active || !ShouldSuppress(config, phase))
		{
			return;
		}

		_timer += deltaTime;
		if (_timer < SweepInterval)
		{
			return;
		}

		_timer = 0f;
		Sweep();
	}

	/// <summary>
	/// Removes every monster, prop and loose pickup currently in the arena.
	/// </summary>
	public static int Sweep()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.instance;
		if (actors == null)
		{
			return 0;
		}

		DoomedEntities.Clear();
		DoomedActors.Clear();

		foreach (Entity entity in actors.allEntities)
		{
			if (!entity.IsNullOrInactive() && IsPve(entity))
			{
				DoomedEntities.Add(entity);
			}
		}

		// Treasures and loose items are Actors, not Entities, so they need a
		// second pass over the wider collection.
		foreach (Actor actor in actors.allActors)
		{
			if (!actor.IsNullOrInactive() && IsUnwantedActor(actor))
			{
				DoomedActors.Add(actor);
			}
		}

		// Destroy, not Kill: a kill would drop rewards and, for a boss, run the
		// defeat path and open the exit rift.
		foreach (Entity entity in DoomedEntities)
		{
			entity.Destroy();
		}

		foreach (Actor actor in DoomedActors)
		{
			actor.Destroy();
		}

		int total = DoomedEntities.Count + DoomedActors.Count;
		if (total > 0)
		{
			Debug.Log($"{ModIdentity.LogPrefix}swept {total} PvE object(s) from the arena.");
		}

		return total;
	}

	/// <summary>
	/// Room furniture: shrines, chests, artifacts, traps and loose items.
	/// </summary>
	/// <remarks>
	/// <para>
	/// These all derive from <c>Actor</c> rather than <c>Entity</c>, which is
	/// why an earlier entity-only sweep left shrines standing in the arena.
	/// </para>
	/// <para>
	/// <c>Rift</c> is deliberately spared — it is the way out of the room.
	/// So are <c>AbilityTrigger</c>, <c>AbilityInstance</c>, <c>GameEffect</c>
	/// and <c>ServerActor</c>, which are the players' own kit and the machinery
	/// running the match.
	/// </para>
	/// <para>
	/// Items are matched through <c>IItem</c>, which a hero's equipped skills
	/// also implement, so only genuinely unclaimed ones are taken:
	/// <c>handOwner</c>, <c>owner</c> and <c>tempOwner</c> must all be empty.
	/// Getting that wrong would strip the players' kits.
	/// </para>
	/// </remarks>
	private static bool IsUnwantedActor(Actor actor)
	{
		if (actor is Rift)
		{
			return false;
		}

		if (actor is Shrine || actor is Treasure || actor is Artifact
		    || actor is Room_Trap_Toggleable)
		{
			return true;
		}

		if (actor is IItem item)
		{
			return item.handOwner == null && item.owner == null && item.tempOwner == null;
		}

		return false;
	}

	/// <summary>
	/// PvE content: monsters (bosses included, since <c>BossMonster</c> derives
	/// from <c>Monster</c>), props such as stones and merchants, and any summon
	/// that does not belong to a player.
	/// </summary>
	/// <remarks>
	/// Only five things derive from <c>Entity</c> — <c>Hero</c>, <c>Summon</c>,
	/// <c>Monster</c>, <c>PropEntity</c> and <c>MockEntity</c> — so this covers
	/// everything a hero could legally auto-attack except other players.
	///
	/// Summons need the ownership test rather than a blanket skip: a monster's
	/// summon outlives the monster being destroyed, stays hostile, and is a
	/// perfectly good auto-attack target even when there is nothing obvious on
	/// screen. Players' own summons must survive.
	/// </remarks>
	private static bool IsPve(Entity entity)
	{
		if (entity is Hero)
		{
			return false;
		}

		if (entity is Summon)
		{
			return !IsOwnedByPlayer(entity);
		}

		return entity is Monster || entity is PropEntity;
	}

	/// <summary>True when a human player owns this entity.</summary>
	private static bool IsOwnedByPlayer(Entity entity)
	{
		DewPlayer owner = entity.owner;
		return owner != null && owner.isHumanPlayer;
	}

	public static void Reset()
	{
		_timer = 0f;
	}
}
