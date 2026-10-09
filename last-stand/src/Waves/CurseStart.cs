namespace LastStand.Waves;

internal enum CurseStartEvent
{
	None,
	HoldStarted,
	StartedWithoutCurse,
}

internal sealed class CurseStart
{
	private bool _chosen;
	private bool _inBreak;

	public CurseStartEvent Tick(double now, WaveSettings settings, WaveDirector waves, bool shrineReady)
	{
		if (waves.WaveInProgress)
		{
			bool skipped = _inBreak && !_chosen && settings.CurseToStart;
			_inBreak = false;
			_chosen = false;
			return skipped ? CurseStartEvent.StartedWithoutCurse : CurseStartEvent.None;
		}

		_inBreak = true;
		if (settings.CurseToStart && shrineReady && !_chosen)
		{
			if (waves.WaitingForCurse)
			{
				return CurseStartEvent.None;
			}

			waves.HoldForCurse();
			return CurseStartEvent.HoldStarted;
		}

		if (waves.WaitingForCurse)
		{
			waves.ReleaseCurseHold(now + settings.IntermissionSeconds);
		}

		return CurseStartEvent.None;
	}

	public bool TryRelease(double now, WaveSettings settings, WaveDirector waves)
	{
		if (!settings.CurseToStart || !waves.WaitingForCurse)
		{
			return false;
		}

		_chosen = true;
		waves.ReleaseCurseHold(now + settings.CurseStartDelaySeconds);
		return true;
	}

	public void Reset()
	{
		_chosen = false;
		_inBreak = false;
	}
}
