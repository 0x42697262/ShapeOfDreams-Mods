namespace SodLib.Game;

internal static class TravelVotes
{
	private static ZoneManager _turnedOff;

	public static void Tick(bool skip)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (!skip || zones == null)
		{
			Release();
			return;
		}

		if (zones.forceVoteForDebug == false)
		{
			return;
		}

		zones.forceVoteForDebug = false;
		_turnedOff = zones;
		Log.Info(SharedLogText.TravelVotesOff);
	}

	public static void Release()
	{
		if (_turnedOff != null && _turnedOff.forceVoteForDebug == false)
		{
			_turnedOff.forceVoteForDebug = null;
		}

		_turnedOff = null;
	}
}
