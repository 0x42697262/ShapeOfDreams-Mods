using System;
using LastStand.Config;
using LastStand.Text;

namespace LastStand.Game;

internal sealed class ContinueCheckpoint
{
	private const int NoWave = 0;

	private readonly ErrorThrottle _errors = new ErrorThrottle(SharedTuning.ErrorLogIntervalSeconds);
	private int _pendingWave = NoWave;
	private double _nextTryAt;

	public void Request(int clearedWave)
	{
		_pendingWave = clearedWave;
		_nextTryAt = 0;
	}

	public void Tick(double now)
	{
		if (_pendingWave == NoWave || now < _nextTryAt)
		{
			return;
		}

		_nextTryAt = now + Tuning.CheckpointRetrySeconds;
		try
		{
			GameManager game = NetworkedManagerBase<GameManager>.softInstance;
			if (game == null || !game.IsEligibleForMidRunSave())
			{
				return;
			}

			game.SaveContinueDataMidRun();
			Log.Info(LogText.CheckpointSaved(_pendingWave));
			_pendingWave = NoWave;
		}
		catch (Exception e)
		{
			_errors.Report(now, LogText.CheckpointFailed, e);
		}
	}

	public void Reset()
	{
		if (_pendingWave != NoWave)
		{
			Log.Warn(LogText.CheckpointMissed(_pendingWave));
		}

		_pendingWave = NoWave;
	}
}
