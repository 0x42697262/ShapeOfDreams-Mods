using System;
using LastStand.Arena;
using LastStand.Diagnostics;
using LastStand.Text;
using Mirror;

namespace LastStand.Combat;

internal sealed class ArenaModifier
{
	private int _node = ArenaLocator.NoNode;
	private string _type;
	private Action<RoomModifierBase> _configure;
	private int _id;
	private RoomModifierBase _instance;

	public bool IsActive { get; private set; }

	public bool Add(string type, Action<RoomModifierBase> configure = null)
	{
		Remove();

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || !ArenaLocator.IsArenaNode(zones, zones.currentNodeIndex))
		{
			return false;
		}

		_node = zones.currentNodeIndex;
		_type = type;
		_configure = configure;
		_id = ModifierGate.AddOwn(zones, _node, _type, Remember);
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

		_id = ModifierGate.AddOwn(zones, _node, _type, Remember);
	}

	public void Remove()
	{
		if (!IsActive)
		{
			return;
		}

		int node = _node;
		int id = _id;
		_instance = null;
		IsActive = false;
		_node = ArenaLocator.NoNode;

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (NetworkServer.active && zones != null && node >= 0 && node < zones.nodes.Count && IsOnNode(zones, node, id))
		{
			try
			{
				zones.RemoveModifier(node, id);
			}
			catch (Exception e)
			{
				Log.Error(LogText.ModifierRemoveFailed(_type), e);
			}
		}
	}

	public bool Owns(RoomModifierBase modifier)
	{
		return modifier != null && modifier == _instance;
	}

	private bool IsOnNode(ZoneManager zones)
	{
		return _node >= 0 && _node < zones.nodes.Count && IsOnNode(zones, _node, _id);
	}

	private static bool IsOnNode(ZoneManager zones, int node, int id)
	{
		return zones.nodes[node].modifiers != null && zones.nodes[node].modifiers.FindIndex(m => m.id == id) >= 0;
	}

	private void Remember(RoomModifierBase modifier)
	{
		_configure?.Invoke(modifier);
		_instance = modifier;
	}
}
