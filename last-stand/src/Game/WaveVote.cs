using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LastStand.Config;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Game;

internal sealed class WaveVote
{
	private const int NoVoteData = -1;

	private static readonly MethodInfo SetVoting =
		AccessTools.PropertySetter(typeof(ZoneManager), nameof(ZoneManager.isVoting));

	private static readonly MethodInfo SetRemainingSeconds =
		AccessTools.PropertySetter(typeof(ZoneManager), nameof(ZoneManager.voteRemainingSeconds));

	private static readonly MethodInfo SetVoteData =
		AccessTools.PropertySetter(typeof(ZoneManager), nameof(ZoneManager.voteData));

	private static readonly MethodInfo SendStarted = AccessTools.Method(typeof(ZoneManager), GameNames.VoteStartedRpc);

	private static readonly MethodInfo SendCompleted =
		AccessTools.Method(typeof(ZoneManager), GameNames.VoteCompletedRpc);

	private readonly ErrorThrottle _errors = new ErrorThrottle(SharedTuning.ErrorLogIntervalSeconds);
	private readonly List<DewPlayer> _voters = new List<DewPlayer>();
	private ZoneManager _open;
	private int _shownSeconds;
	private bool _spent;

	public bool IsOpen => _open != null;

	public WaveVoteResult Tick(int wave, int secondsLeft)
	{
		try
		{
			return TickOpen(wave, secondsLeft);
		}
		catch (Exception e)
		{
			_spent = true;
			_errors.Report(Time.time, LogText.WaveVoteFailed, e);
			return WaveVoteResult.Pending;
		}
	}

	public bool Reopen(int wave, int secondsLeft)
	{
		try
		{
			ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
			if (zones == null || zones.isVoting || secondsLeft <= Tuning.WaveVoteMinSeconds)
			{
				return false;
			}

			Open(zones, wave, secondsLeft);
			return _open != null;
		}
		catch (Exception e)
		{
			_errors.Report(Time.time, LogText.WaveVoteFailed, e);
			return false;
		}
	}

	public void Close()
	{
		_spent = false;
		ZoneManager zones = _open;
		_open = null;
		if (zones == null || !zones.isVoting)
		{
			return;
		}

		try
		{
			SetVoting.Invoke(zones, new object[] { false });
			SetVoteData.Invoke(zones, new object[] { NoVoteData });
			SendCompleted.Invoke(zones, Array.Empty<object>());
		}
		catch (Exception e)
		{
			_errors.Report(Time.time, LogText.WaveVoteFailed, e);
		}
	}

	private WaveVoteResult TickOpen(int wave, int secondsLeft)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return WaveVoteResult.Pending;
		}

		if (_open == null)
		{
			if (!_spent && secondsLeft > Tuning.WaveVoteMinSeconds && !zones.isVoting)
			{
				Open(zones, wave, secondsLeft);
			}

			return WaveVoteResult.Pending;
		}

		if (_open != zones || !zones.isVoting)
		{
			_open = null;
			return WaveVoteResult.Canceled;
		}

		ShowSeconds(secondsLeft);
		return EveryoneReady() ? WaveVoteResult.Passed : WaveVoteResult.Pending;
	}

	private void Open(ZoneManager zones, int wave, int secondsLeft)
	{
		DewPlayer starter = Starter();
		_spent = true;
		if (starter == null)
		{
			return;
		}

		_open = zones;
		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null)
			{
				player.isReady = false;
			}
		}

		zones.NetworkvoteType = VoteType.None;
		SetVoteData.Invoke(zones, new object[] { NoVoteData });
		_shownSeconds = secondsLeft;
		SetRemainingSeconds.Invoke(zones, new object[] { secondsLeft });
		SetVoting.Invoke(zones, new object[] { true });
		SendStarted.Invoke(zones, new object[] { starter });
		Log.Info(LogText.WaveVoteOpened(wave, secondsLeft));
	}

	private void ShowSeconds(int secondsLeft)
	{
		if (secondsLeft == _shownSeconds)
		{
			return;
		}

		_shownSeconds = secondsLeft;
		SetRemainingSeconds.Invoke(_open, new object[] { secondsLeft });
	}

	private bool EveryoneReady()
	{
		PlayerRoster.FillPlayersWithLiveHeroes(_voters);
		if (_voters.Count == 0)
		{
			return false;
		}

		foreach (DewPlayer voter in _voters)
		{
			if (!voter.isReady)
			{
				return false;
			}
		}

		return true;
	}

	private static DewPlayer Starter()
	{
		if (DewPlayer.local != null)
		{
			return DewPlayer.local;
		}

		return DewPlayer.gamePlayers != null && DewPlayer.gamePlayers.Count > 0 ? DewPlayer.gamePlayers[0] : null;
	}
}
