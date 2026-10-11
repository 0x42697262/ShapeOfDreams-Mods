using LastStand.Config;
using LastStand.Text;

namespace LastStand.Game;

internal sealed class MidJoinGate
{
	private double _nextCheckAt;

	public void Tick(double now)
	{
		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.MidJoinCheckSeconds;

		GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
		if (settings == null || !IsBossWait(settings.midJoinWaitType))
		{
			return;
		}

		MidJoinWaitType previous = settings.midJoinWaitType;
		settings.midJoinWaitType = MidJoinWaitType.None;
		Log.Info(LogText.MidJoinOpened(previous));
	}

	public void Reset()
	{
		_nextCheckAt = 0;
	}

	private static bool IsBossWait(MidJoinWaitType wait)
	{
		return wait >= MidJoinWaitType.BeforeBossFight && wait <= MidJoinWaitType.AfterBossFight;
	}
}
