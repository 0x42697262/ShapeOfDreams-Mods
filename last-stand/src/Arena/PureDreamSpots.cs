using System;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Arena;

internal static class PureDreamSpots
{
	private static readonly ErrorThrottle Errors = new ErrorThrottle(Tuning.ErrorLogIntervalSeconds);

	public static bool TryPick(Func<Vector3> roll, out Vector3 spot)
	{
		spot = Vector3.zero;
		try
		{
			spot = Pick(roll);
			return true;
		}
		catch (Exception e)
		{
			Errors.Report(Time.time, LogText.PureDreamSpotFailed, e);
			return false;
		}
	}

	public static bool TryPickAnywhere(out Vector3 spot)
	{
		return TryPick(RandomRoomNode, out spot);
	}

	private static Vector3 RandomRoomNode()
	{
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		RoomSection section = room.sections[UnityEngine.Random.Range(0, room.sections.Count)];
		return section.GetAnyRandomNode();
	}

	private static Vector3 Pick(Func<Vector3> roll)
	{
		Vector3 spot = roll();
		if (!ArenaLayout.TryFind(out ArenaPoints arena))
		{
			return spot;
		}

		int tries = 1;
		while (!IsGood(arena.Center, spot))
		{
			if (tries >= Tuning.PureDreamSpotAttempts)
			{
				Vector3 level = new Vector3(spot.x, arena.Center.y, spot.z);
				Vector3 pulled = PlayableGround.PullToward(arena.Center, level);
				Log.Info(LogText.PureDreamSpotPulled(tries, spot, pulled));
				return pulled;
			}

			spot = roll();
			tries++;
		}

		if (tries > 1)
		{
			Log.Info(LogText.PureDreamSpotRerolled(tries, spot));
		}

		return spot;
	}

	private static bool IsGood(Vector3 center, Vector3 spot)
	{
		return PlayableGround.IsPlayable(spot) && PlayableGround.IsReachable(center, spot);
	}
}
