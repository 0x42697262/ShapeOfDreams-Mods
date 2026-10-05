using System.Linq;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Puts a <c>-restart</c> to the party through the game's own travel vote:
/// Tab to agree, Esc to cancel, and it goes ahead when the countdown runs out.
/// </summary>
/// <remarks>
/// <para>
/// The vote is the stock one, started with
/// <c>ZoneManager.StartVoteNextZone</c>, so vanilla clients get the usual
/// panel, countdown and keys with nothing to install. The requester counts as
/// having agreed. Any player pressing Esc cancels it
/// (<c>CmdCancelVote</c>), everyone agreeing passes it early, and the
/// countdown running out passes it.
/// </para>
/// <para>
/// The next-zone flavor is used rather than next-node because the next-node
/// panel draws the mini world map, every unused node included. The next-zone
/// panel shows no map.
/// </para>
/// <para>
/// While a restart vote is open, <see cref="Pending"/> is set, and three
/// patches in <c>Patches.RestartVotePatches</c> read it:
/// </para>
/// <list type="bullet">
/// <item><description>A passed next-zone vote calls
/// <c>GameManager.LoadNextZone</c>. The prefix runs the restart instead and
/// skips the zone change.</description></item>
/// <item><description><c>ZoneManager.ClearVoteState</c> runs on cancel and
/// after completion, and clears the flag.</description></item>
/// <item><description><c>ZoneManager.GetCannotTravelReason</c> is waived. It
/// holds a vote until nobody carries an item and nothing is left on the floor,
/// both of which the restart clears anyway.</description></item>
/// </list>
/// </remarks>
internal static class RestartVote
{
	/// <summary>A restart vote is open.</summary>
	public static bool Pending { get; private set; }

	private static DewPlayer _requester;

	/// <summary>
	/// Starts a restart vote for <paramref name="player"/>, or restarts at once
	/// when they are the only hero standing.
	/// </summary>
	public static bool TryBegin(DewPlayer player, out string reason)
	{
		if (!MatchRestart.CanRestart(out reason))
		{
			return false;
		}

		int voters = DewPlayer.gamePlayers.Count(p => !p.hero.IsNullInactiveDeadOrKnockedOut());
		if (voters <= 1)
		{
			// Nobody to ask. The stock vote would just sit out its countdown.
			return MatchRestart.Execute(player, out reason);
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		Pending = true;
		_requester = player;
		zones.StartVoteNextZone(player);

		if (!zones.isVoting)
		{
			Pending = false;
			_requester = null;
			reason = "The vote could not start.";
			return false;
		}

		string who = player != null ? ChatManager.GetDescribedPlayerName(player) : "Someone";
		MatchRestart.Broadcast($"{who} wants to restart: back to camp, builds wiped. " +
		                       "Tab to agree, Esc to cancel.");
		Debug.Log($"{ModIdentity.LogPrefix}restart vote started by {(player != null ? player.guid : "?")}.");
		return true;
	}

	/// <summary>
	/// The vote passed. Returns true when it was ours, in which case the
	/// restart has run and the zone change must be skipped.
	/// </summary>
	public static bool TryConsumePassed()
	{
		if (!Pending || !NetworkServer.active)
		{
			return false;
		}

		DewPlayer requester = _requester;
		Pending = false;
		_requester = null;

		if (!MatchRestart.Execute(requester, out string reason))
		{
			MatchRestart.Broadcast($"Restart failed: {reason}");
			Debug.LogWarning($"{ModIdentity.LogPrefix}restart vote passed but failed: {reason}");
		}

		return true;
	}

	/// <summary>The vote ended one way or the other.</summary>
	public static void OnVoteCleared()
	{
		Pending = false;
		_requester = null;
	}

	public static void Reset()
	{
		Pending = false;
		_requester = null;
	}
}
