using System;

namespace LastStand.Diagnostics;

internal sealed class ErrorThrottle
{
	private readonly double _intervalSeconds;
	private double _nextAt;
	private int _suppressed;

	public ErrorThrottle(double intervalSeconds)
	{
		_intervalSeconds = intervalSeconds;
	}

	public void Report(double now, string message, Exception exception)
	{
		if (now < _nextAt)
		{
			_suppressed++;
			return;
		}

		Log.Error(Text.LogText.WithSuppressed(message, _suppressed), exception);
		_suppressed = 0;
		_nextAt = now + _intervalSeconds;
	}
}
