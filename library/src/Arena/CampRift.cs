namespace SodLib.Arena;

internal sealed class CampRift
{
	private readonly bool _hideOtherMapNodes;
	private double _nextCheckAt;
	private int _target = ArenaLocator.NoNode;

	public CampRift(bool hideOtherMapNodes)
	{
		_hideOtherMapNodes = hideOtherMapNodes;
	}

	public void Tick(double now)
	{
		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + SharedTuning.CampRiftCheckSeconds;
		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (rift.IsNullOrInactive() || zones == null || !ArenaLocator.TryFindArenaNode(zones, out int target))
		{
			return;
		}

		if (_hideOtherMapNodes)
		{
			WorldMapNodes.HideAllBut(zones, target);
		}

		if (rift.nextNodeIndex == target && _target == target)
		{
			return;
		}

		rift.NetworknextNodeIndex = target;
		_target = target;
		Log.Info(SharedLogText.CampRiftDirected(target));
	}

	public void Reset()
	{
		_nextCheckAt = 0;
		_target = ArenaLocator.NoNode;
	}
}
