using LastStand.Game;
using LastStand.Text;

namespace LastStand.Combat;

internal sealed class ArenaPanel
{
	private readonly ArenaModifier _modifier = new ArenaModifier();

	public void Tick(bool otherModifierActive)
	{
		if (otherModifierActive)
		{
			_modifier.Remove();
			return;
		}

		if (_modifier.IsActive)
		{
			_modifier.Restore();
		}
		else if (_modifier.Add(GameNames.ArenaPanelModifier))
		{
			Log.Info(LogText.ArenaPanelShown(GameNames.ArenaPanelModifier));
		}

		ForgetSidetrackRift();
	}

	public void Reset()
	{
		_modifier.Remove();
	}

	private static void ForgetSidetrackRift()
	{
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room != null && room.rifts != null)
		{
			room.rifts.openedSidetrackRifts.Remove(GameNames.ArenaPanelSidetrackRift);
		}
	}
}
