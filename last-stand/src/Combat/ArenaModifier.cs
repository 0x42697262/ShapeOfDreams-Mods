using LastStand.Arena;

namespace LastStand.Combat;

internal sealed class ArenaModifier<T> where T : RoomModifierBase
{
	private int _node = ArenaLocator.NoNode;
	private int _id;
	private RoomModifierBase _instance;

	public bool IsActive { get; private set; }

	public bool Add()
	{
		Remove();

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || !ArenaLocator.IsArenaNode(zones, zones.currentNodeIndex))
		{
			return false;
		}

		_node = zones.currentNodeIndex;
		_id = zones.AddModifier<T>(_node, Remember);
		IsActive = true;
		return true;
	}

	public void Restore()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (!IsActive || zones == null || zones.currentNodeIndex != _node || IsOnNode(zones))
		{
			return;
		}

		_id = zones.AddModifier<T>(_node, Remember);
	}

	public void Remove()
	{
		if (!IsActive)
		{
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones != null && _node >= 0 && _node < zones.nodes.Count)
		{
			zones.RemoveModifier(_node, _id);
		}

		IsActive = false;
		_node = ArenaLocator.NoNode;
	}

	public bool Owns(RoomModifierBase modifier)
	{
		return modifier != null && modifier == _instance;
	}

	private bool IsOnNode(ZoneManager zones)
	{
		return _node >= 0 && _node < zones.nodes.Count &&
		       zones.nodes[_node].modifiers.FindIndex(m => m.id == _id) >= 0;
	}

	private void Remember(T modifier)
	{
		_instance = modifier;
	}
}
