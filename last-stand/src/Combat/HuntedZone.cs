using Mirror;

namespace LastStand.Combat;

internal sealed class HuntedZone
{
	private readonly ArenaModifier _modifier = new ArenaModifier();
	private int _previousHuntLevel;

	public bool IsActive => _modifier.IsActive;

	public void Begin(int huntLevel)
	{
		End();

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return;
		}

		_previousHuntLevel = zones.currentHuntLevel;
		zones.currentHuntLevel = huntLevel;
		if (!_modifier.Add(nameof(RoomMod_Hunted), KeepArtilleryWithoutRift))
		{
			zones.currentHuntLevel = _previousHuntLevel;
		}
	}

	private static void KeepArtilleryWithoutRift(RoomModifierBase modifier)
	{
		if (modifier is RoomMod_Hunted hunted)
		{
			hunted.endWhenRiftOpen = false;
		}
	}

	public void Restore()
	{
		_modifier.Restore();
	}

	public void End()
	{
		if (!_modifier.IsActive)
		{
			return;
		}

		_modifier.Remove();

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones != null && NetworkServer.active)
		{
			zones.currentHuntLevel = _previousHuntLevel;
		}
	}
}
