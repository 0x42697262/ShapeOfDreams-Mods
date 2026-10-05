namespace LastStand.Combat;

internal sealed class HuntedZone
{
	private readonly ArenaModifier<RoomMod_Hunted> _modifier = new ArenaModifier<RoomMod_Hunted>();
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
		if (!_modifier.Add())
		{
			zones.currentHuntLevel = _previousHuntLevel;
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
		if (zones != null)
		{
			zones.currentHuntLevel = _previousHuntLevel;
		}
	}
}
