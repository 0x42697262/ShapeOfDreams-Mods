using LastStand.Diagnostics;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class HatredShrine
{
	private const int NotFound = -1;

	private static readonly HatredStrengthType[] Curses =
	{
		HatredStrengthType.Mild, HatredStrengthType.Potent, HatredStrengthType.Powerful,
	};

	private readonly StandingShrine<Shrine_Hatred> _standing =
		new StandingShrine<Shrine_Hatred>(LogText.HatredRemnantName, ShrineUses.Vanilla, float.PositiveInfinity);

	public void Place(Vector3 position)
	{
		_standing.Place(position);
	}

	public bool IsStanding => !_standing.Current.IsNullOrInactive();

	public void ResetUses()
	{
		Shrine_Hatred shrine = _standing.Current;
		if (!shrine.IsNullOrInactive())
		{
			shrine.ResetUseCounts();
		}
	}

	public void RequireCurses()
	{
		Shrine_Hatred shrine = _standing.Current;
		if (shrine.IsNullOrInactive())
		{
			return;
		}

		int none = shrine.choices.IndexOf(HatredStrengthType.None);
		if (none == NotFound)
		{
			return;
		}

		foreach (HatredStrengthType strength in Curses)
		{
			if (!shrine.choices.Contains(strength))
			{
				shrine.choices[none] = strength;
				Log.Info(LogText.HatredChoicesNeedCurse(HatredStrengthType.None.ToString(), strength.ToString()));
				return;
			}
		}
	}

	public void OnWaveCleared(int wave)
	{
		Shrine_Hatred shrine = _standing.Current;
		if (shrine.IsNullOrInactive())
		{
			return;
		}

		shrine.ResetUseCounts();
		Log.Info(LogText.HatredUsesReset(wave));
	}

	public void Forget()
	{
		_standing.Forget();
	}
}
