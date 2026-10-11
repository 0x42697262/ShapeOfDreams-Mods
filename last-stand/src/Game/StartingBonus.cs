using System;
using System.Globalization;
using LastStand.Text;

namespace LastStand.Game;

internal sealed class StartingBonus
{
	private GameManager _handled;

	public void Tick(int gold, int dreamDust)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null || game == _handled || !game.IsLazyCallReady())
		{
			return;
		}

		_handled = game;
		string run = game.GetInstanceID().ToString(CultureInfo.InvariantCulture);
		if ((gold <= 0 && dreamDust <= 0) || DewNetworkManager.startSettings.continueData != null
		    || AppDomain.CurrentDomain.GetData(ModIdentity.StartingGoldSlot) as string == run)
		{
			return;
		}

		AppDomain.CurrentDomain.SetData(ModIdentity.StartingGoldSlot, run);
		if (gold > 0)
		{
			Log.Info(LogText.StartingGoldGranted(gold, PlayerRoster.GiveGold(gold)));
		}

		if (dreamDust > 0)
		{
			Log.Info(LogText.StartingDreamDustGranted(dreamDust, PlayerRoster.GiveDreamDust(dreamDust)));
		}
	}
}
