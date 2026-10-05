using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Sends everyone from the arena back to camp with a clean slate: builds
/// wiped, starting loadout restored, remnants fully restocked.
/// </summary>
/// <remarks>
/// <para>
/// Runs when a <c>-restart</c> vote passes (see <see cref="RestartVote"/>),
/// or straight away from the host's <c>arena_restart</c> console command.
/// Everything runs on the host, so vanilla clients only see the synced
/// results.
/// </para>
/// <para>
/// Order matters:
/// </para>
/// <list type="number">
/// <item><description>Knocked-out heroes are revived, so nobody arrives in camp
/// still down. A room travel does not revive anyone; only a zone travel
/// does.</description></item>
/// <item><description>Every equipped essence and the Q, W, E and R memories are
/// unequipped and destroyed rather than dropped. Identity and movement cannot
/// be removed, so they stay, but their essences go.</description></item>
/// <item><description>The hero's starting Q and R memories are created again
/// from its loadout, as <c>HeroSkill.OnLateStartServer</c> does at the start
/// of a run, so nobody is left with no skills at all.
/// <c>HeroLeveller</c> raises them to the configured level as
/// usual.</description></item>
/// <item><description>Remnant bookkeeping is reset, so every item is offered
/// again and the camp builds its remnants anew.</description></item>
/// <item><description>Every room's saved state is dropped
/// (<c>visitedNodesSaveData</c>). A revisited room otherwise restores what was
/// left in it: the camp would come back with its old, half-picked remnants and
/// the arena with whatever memories were lying on its floor.</description></item>
/// <item><description>Everyone travels to the camp node.
/// <c>TravelToNode</c> is the server-side call and does not require the nodes
/// to be connected; the arena is not linked back to camp.</description></item>
/// </list>
/// </remarks>
internal static class MatchRestart
{
	private const string NoticeColor = "#f5c542";

	private static readonly HeroSkillLocation[] MemorySlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.W,
		HeroSkillLocation.E,
		HeroSkillLocation.R,
	};

	private static readonly HeroSkillLocation[] AllSlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.W,
		HeroSkillLocation.E,
		HeroSkillLocation.R,
		HeroSkillLocation.Identity,
		HeroSkillLocation.Movement,
	};

	/// <summary>Starting memories that come from the hero's loadout.</summary>
	private static readonly HeroSkillLocation[] LoadoutMemorySlots =
	{
		HeroSkillLocation.Q,
		HeroSkillLocation.R,
	};

	/// <summary>Restarts right away, skipping the vote. For the host's console.</summary>
	public static bool TryRestart(DewPlayer requester, out string reason)
	{
		if (!CanRestart(out reason))
		{
			return false;
		}

		return Execute(requester, out reason);
	}

	/// <summary>
	/// Whether a restart could start now. The reason is meant for the player
	/// who asked.
	/// </summary>
	public static bool CanRestart(out string reason)
	{
		reason = null;

		DreamArenaMod mod = DreamArenaMod.Instance;
		if (!NetworkServer.active || mod == null || !mod.config.arenaEnabled)
		{
			reason = "PvP mode is off.";
			return false;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			reason = "The game is not ready yet.";
			return false;
		}

		if (zones.isInAnyTransition)
		{
			reason = "Already traveling.";
			return false;
		}

		if (zones.isVoting)
		{
			reason = "A vote is already running.";
			return false;
		}

		if (mod.Phase != PhaseKind.Arena)
		{
			reason = "Already in camp. -restart works from the arena.";
			return false;
		}

		if (FindCampNode(zones) < 0)
		{
			reason = "Could not find the camp.";
			return false;
		}

		return true;
	}

	/// <summary>
	/// Wipes every build and sends everyone to camp. Called once the vote has
	/// passed, so it only rechecks what a vote cannot guarantee.
	/// </summary>
	public static bool Execute(DewPlayer requester, out string reason)
	{
		reason = null;

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		int camp = zones != null ? FindCampNode(zones) : -1;
		if (!NetworkServer.active || camp < 0 || zones.isInAnyTransition)
		{
			reason = "The game is not ready to travel.";
			return false;
		}

		var heroes = new List<Hero>();
		foreach (DewPlayer player in DewPlayer.gamePlayers.ToList())
		{
			if (!player.hero.IsNullOrInactive())
			{
				heroes.Add(player.hero);
			}
		}

		foreach (Hero hero in heroes)
		{
			Revive(hero);
			hero.Skill.StopHoldInHand();
			WipeBuild(hero);
		}

		// Before the loadout comes back: a freshly created memory has no owner
		// until it is equipped, and would be swept up with the rest.
		int swept = SweepLooseItems();

		foreach (Hero hero in heroes)
		{
			RestoreLoadout(hero);
		}

		CampShops.Reset();
		ForgetRoomStates(zones);

		zones.TravelToNode(camp, advanceTurn: false, ignoreInterrupts: true);

		Broadcast("Restarting. Back to camp: builds wiped, remnants restocked.");
		Debug.Log($"{ModIdentity.LogPrefix}restart by {(requester != null ? requester.guid : "host")}: " +
		          $"wiped {heroes.Count} hero(es), swept {swept} loose item(s), " +
		          $"traveling to camp node {camp}.");
		return true;
	}

	/// <summary>
	/// Destroys every memory and essence nobody has equipped: ones lying on the
	/// floor and the one a hero was carrying.
	/// </summary>
	/// <remarks>
	/// Clearing the saved room states is not enough on its own. The room being
	/// left is serialized during the trip, after this runs, so anything still on
	/// the arena floor would be saved again and restored on the next visit.
	/// </remarks>
	private static int SweepLooseItems()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int swept = 0;
		foreach (Actor actor in actors.allActors.ToList())
		{
			bool loose = (actor is SkillTrigger skill && skill.owner == null)
			             || (actor is Gem gem && gem.owner == null);
			if (!loose || actor.IsNullOrInactive())
			{
				continue;
			}

			actor.Destroy();
			swept++;
		}

		return swept;
	}

	private static int FindCampNode(ZoneManager zones)
	{
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (zones.nodes[i].type == WorldNodeType.Start)
			{
				return i;
			}
		}

		return -1;
	}

	private static void Revive(Hero hero)
	{
		if (hero.isKnockedOut
		    && hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
		{
			knockedOut.Revive(1f);
		}
	}

	private static void WipeBuild(Hero hero)
	{
		HeroSkill skills = hero.Skill;
		Vector3 at = hero.position;

		// Essences first: they sit in slots owned by the hero, not by the
		// memory, so removing a memory leaves its essences equipped.
		var gemSlots = new List<GemLocation>();
		foreach (HeroSkillLocation slot in AllSlots)
		{
			foreach (KeyValuePair<GemLocation, Gem> pair in skills.GetGemsPairInSkill(slot))
			{
				gemSlots.Add(pair.Key);
			}
		}

		foreach (GemLocation loc in gemSlots)
		{
			Gem gem = skills.UnequipGem(loc, at);
			if (!gem.IsNullOrInactive())
			{
				gem.Destroy();
			}
		}

		foreach (HeroSkillLocation slot in MemorySlots)
		{
			if (!skills.TryGetSkill(slot, out SkillTrigger _))
			{
				continue;
			}

			SkillTrigger removed = skills.UnequipSkill(slot, at, ignoreCanReplace: true);
			if (!removed.IsNullOrInactive())
			{
				removed.Destroy();
			}
		}
	}

	/// <summary>
	/// Recreates the starting memories, the way the game equips them when a
	/// hero first spawns.
	/// </summary>
	private static void RestoreLoadout(Hero hero)
	{
		if (hero.loadout == null)
		{
			return;
		}

		foreach (HeroSkillLocation slot in LoadoutMemorySlots)
		{
			int choice = hero.loadout.GetSkill(slot);
			if (choice < 0)
			{
				continue;
			}

			SkillTrigger[] options = hero.Skill.GetLoadoutSkills(slot);
			if (options == null || options.Length == 0)
			{
				continue;
			}

			SkillTrigger prefab = options[Mathf.Clamp(choice, 0, options.Length - 1)];
			SkillTrigger created = Dew.CreateSkillTrigger(prefab, hero.position, 1);
			hero.Skill.EquipSkill(slot, created, ignoreCanReplace: true);
		}
	}

	private static void ForgetRoomStates(ZoneManager zones)
	{
		if (zones.visitedNodesSaveData == null)
		{
			return;
		}

		for (int i = 0; i < zones.visitedNodesSaveData.Count; i++)
		{
			zones.visitedNodesSaveData[i] = null;
		}
	}

	/// <summary>Posts a line in everyone's chat. Plain text, so vanilla clients show it.</summary>
	public static void Broadcast(string text)
	{
		ChatManager chat = NetworkedManagerBase<ChatManager>.softInstance;
		if (chat == null)
		{
			return;
		}

		chat.BroadcastMessage(new ChatManager.Message
		{
			type = ChatManager.MessageType.Raw,
			content = $"<color={NoticeColor}>[Dream Arena] {text}</color>",
		});
	}

	/// <summary>Posts a line in one player's chat only.</summary>
	public static void Tell(DewPlayer player, string text)
	{
		ChatManager chat = NetworkedManagerBase<ChatManager>.softInstance;
		if (chat == null || player == null || player.connectionToClient == null)
		{
			return;
		}

		chat.SendChatMessage(player.connectionToClient, new ChatManager.Message
		{
			type = ChatManager.MessageType.Raw,
			content = $"<color={NoticeColor}>[Dream Arena] {text}</color>",
		});
	}
}
