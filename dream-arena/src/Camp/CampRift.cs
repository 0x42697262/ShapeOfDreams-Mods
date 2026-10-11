using DreamArena.Arena;
using DreamArena.Config;
using DreamArena.Text;

namespace DreamArena.Camp;

internal sealed class CampRift
{
	private double _nextCheckAt;

	public void Tick(double now)
	{
		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.CampRiftCheckSeconds;
		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (rift.IsNullOrInactive() || zones == null || !ArenaLocator.TryFindArenaNode(zones, out int target))
		{
			return;
		}

		if (rift.nextNodeIndex == target)
		{
			return;
		}

		rift.NetworknextNodeIndex = target;
		Log.Info(LogText.CampRiftPointed(target));
	}

	public void Reset()
	{
		_nextCheckAt = 0;
	}
}
