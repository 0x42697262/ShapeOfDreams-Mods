using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;
using Mirror;

namespace LastStand.Game;

internal sealed class GoldPrices
{
	private bool _applied;

	public void Set(float multiplier, int wave)
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game == null || !NetworkServer.active)
		{
			return;
		}

		_applied = true;
		if (game.goldCostMultiplier == multiplier)
		{
			return;
		}

		game.NetworkgoldCostMultiplier = multiplier;
		Log.Info(LogText.GoldPricesSet(multiplier, wave));
	}

	public void Restore()
	{
		if (!_applied)
		{
			return;
		}

		_applied = false;
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		if (game != null && NetworkServer.active)
		{
			game.NetworkgoldCostMultiplier = Tuning.VanillaGoldCostMultiplier;
			Log.Info(LogText.GoldPricesRestored(Tuning.VanillaGoldCostMultiplier));
		}
	}
}
