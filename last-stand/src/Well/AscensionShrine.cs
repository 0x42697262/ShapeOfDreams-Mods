using LastStand.Text;
using LastStand.Waves;
using UnityEngine;

namespace LastStand.Well;

internal sealed class AscensionShrine
{
	private const int NoWave = -1;

	private readonly StandingShrine<Shrine_Ascension> _standing =
		new StandingShrine<Shrine_Ascension>(LogText.ShrineOfAscensionName, ShrineUses.Vanilla,
			float.PositiveInfinity);

	private int _resetWave = NoWave;

	public void Place(Vector3 position)
	{
		_standing.Place(position);
	}

	public void LockUntilFirstReset(int clearedWaves, Cadence resetWaves)
	{
		Shrine_Ascension shrine = _standing.Current;
		bool locked = clearedWaves < resetWaves.FirstWave;
		if (shrine.IsNullOrInactive() || shrine.isLocked == locked)
		{
			return;
		}

		shrine.isLocked = locked;
		Log.Info(locked ? LogText.AscensionLocked(resetWaves.FirstWave) : LogText.AscensionUnlocked(clearedWaves));
	}

	public void OnWaveCleared(int wave, Cadence resetWaves)
	{
		LockUntilFirstReset(wave, resetWaves);
		Shrine_Ascension shrine = _standing.Current;
		if (shrine.IsNullOrInactive() || !resetWaves.IsOn(wave) || _resetWave == wave)
		{
			return;
		}

		_resetWave = wave;
		shrine.ResetUseCounts();
		Log.Info(LogText.AscensionUsesReset(wave));
	}

	public void Forget()
	{
		_resetWave = NoWave;
		_standing.Forget();
	}
}
