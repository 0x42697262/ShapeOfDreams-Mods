using LastStand.Config;
using LastStand.Text;

namespace LastStand.Game;

internal sealed class StartingWorld
{
	private const int NoZoneYet = 0;

	private double _nextCheckAt;

	public void Tick(double now, int world)
	{
		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.WorldCheckSeconds;

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || zones.currentZone == null || zones.currentZoneIndex < NoZoneYet
		    || zones.isInAnyTransition)
		{
			return;
		}

		int index = world - Tuning.MinStartingWorld;
		int from = zones.currentZoneIndex;
		if (from == index)
		{
			return;
		}

		zones.currentZoneIndex = index;
		Log.Info(LogText.WorldSet(from, index));
	}

	public void Reset()
	{
		_nextCheckAt = 0;
	}
}
