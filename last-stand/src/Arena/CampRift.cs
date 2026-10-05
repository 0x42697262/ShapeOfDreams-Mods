using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;

namespace LastStand.Arena;

internal sealed class CampRift
{
	private double _nextAttemptAt;
	private int _target = ArenaLocator.NoNode;

	public void Tick(double now)
	{
		if (now < _nextAttemptAt)
		{
			return;
		}

		_nextAttemptAt = now + Tuning.CampRiftRetrySeconds;

		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (rift.IsNullOrInactive() || zones == null || !TryFindArenaNode(zones, out int target))
		{
			return;
		}

		if (rift.nextNodeIndex == target && _target == target)
		{
			return;
		}

		rift.NetworknextNodeIndex = target;
		_target = target;
		Log.Info(LogText.CampRiftDirected(target));
	}

	public void Reset()
	{
		_nextAttemptAt = 0;
		_target = ArenaLocator.NoNode;
	}

	private static bool TryFindArenaNode(ZoneManager zones, out int target)
	{
		int current = zones.currentNodeIndex;
		int fallback = ArenaLocator.NoNode;

		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (i == current || !ArenaLocator.IsArenaNode(zones, i) || zones.nodes[i].IsSidetrackNode())
			{
				continue;
			}

			if (current != ArenaLocator.NoNode && zones.IsNodeConnected(current, i))
			{
				target = i;
				return true;
			}

			if (fallback == ArenaLocator.NoNode)
			{
				fallback = i;
			}
		}

		target = fallback;
		return target != ArenaLocator.NoNode;
	}
}
