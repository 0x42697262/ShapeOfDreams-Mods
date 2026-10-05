using LastStand.Arena;
using LastStand.Game;

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
		_id = ModifierGate.AddOwn<T>(zones, _node, Remember);
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

		_id = ModifierGate.AddOwn<T>(zones, _node, Remember);
	}

	public void Remove()
	{
		if (!IsActive)
		{
			return;
		}

		bool shown = !_instance.IsNullOrInactive();
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones != null && _node >= 0 && _node < zones.nodes.Count)
		{
			zones.RemoveModifier(_node, _id);
		}

		if (shown)
		{
			ModifierPanel.Hide();
		}

		_instance = null;
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
