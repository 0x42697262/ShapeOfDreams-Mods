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
	private double _nextFindAt;

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
		string wanted = label(now);
		if (wanted != null && _text == null && now >= _nextFindAt)
		{
			_nextFindAt = now + Tuning.HudFindRetrySeconds;
			Capture(UnityEngine.Object.FindAnyObjectByType<UI_InGame_Location>());
		}

		Show(wanted);
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
