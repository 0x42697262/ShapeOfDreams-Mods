using System;
using LastStand.Config;
using TMPro;

namespace LastStand.UI;

internal sealed class LocationLabel
{
	private TMP_Text _text;
	private string _vanilla;
	private string _wanted;
	private string _shown;
	private double _nextRefreshAt;

	public void Capture(UI_InGame_Location location)
	{
		_text = location != null ? location.GetComponent<TMP_Text>() : null;
		_vanilla = _text != null ? _text.text : null;
		_shown = null;
		Apply();
	}

	public void Tick(double now, Func<double, string> label)
	{
		if (now < _nextRefreshAt)
		{
			return;
		}

		_nextRefreshAt = now + Tuning.HudRefreshSeconds;
		Show(label(now));
	}

	public void Show(string label)
	{
		_wanted = label;
		Apply();
	}

	private void Apply()
	{
		if (_text == null || _wanted == _shown)
		{
			return;
		}

		_text.text = _wanted ?? _vanilla;
		_shown = _wanted;
	}
}
