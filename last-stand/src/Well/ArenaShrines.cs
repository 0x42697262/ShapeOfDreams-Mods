using LastStand.Arena;
using LastStand.Config;
using LastStand.Text;
using LastStand.Waves;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace LastStand.Well;

internal sealed class ArenaShrines
{
	private readonly StandingShrine<Shrine_UpgradeWell> _well =
		new StandingShrine<Shrine_UpgradeWell>(LogText.DreamWellName, ShrineUses.Unlimited,
			float.PositiveInfinity);

	private readonly StandingShrine<Shrine_AltarOfCleansing> _altar =
		new StandingShrine<Shrine_AltarOfCleansing>(LogText.AltarOfCleansingName, ShrineUses.Unlimited,
			float.PositiveInfinity);

	private readonly AscensionShrine _ascension = new AscensionShrine();

	private readonly MawOfDoomShrine _maw = new MawOfDoomShrine();

	private readonly DestinyShrine _destiny = new DestinyShrine();

	private readonly HatredShrine _hatred = new HatredShrine();

	private readonly LuckShrine _luck = new LuckShrine();

	private readonly List<Action<Vector3>> _edgeShrines = new List<Action<Vector3>>();

	private double _nextPinAt;

	public bool HatredStanding => _hatred.IsStanding;

	public void Place(ArenaPoints arena, SessionSettings settings)
	{
		_edgeShrines.Clear();
		if (settings.DreamWell)
		{
			_edgeShrines.Add(_well.Place);
		}
		if (settings.AltarOfCleansing)
		{
			_edgeShrines.Add(_altar.Place);
		}
		if (settings.ShrineOfAscension)
		{
			_edgeShrines.Add(_ascension.Place);
		}
		if (settings.MawOfDoom)
		{
			_edgeShrines.Add(_maw.Place);
		}
		if (settings.ShrineOfDestiny)
		{
			_edgeShrines.Add(_destiny.Place);
		}
		if (settings.HatredRemnant)
		{
			_edgeShrines.Add(_hatred.Place);
		}
		if (settings.ReminiscenceOfLuck)
		{
			_edgeShrines.Add(_luck.Place);
		}
		Vector3[] array = EdgePlacement.PickRing(arena, _edgeShrines.Count);
		for (int i = 0; i < _edgeShrines.Count; i++)
		{
			_edgeShrines[i](array[i]);
		}
	}

	public void OnUsed(Shrine shrine, Entity user, int destinyClaimsPerWave)
	{
		_well.OnUsed(shrine);
		_altar.OnUsed(shrine);
		_maw.OnUsed(shrine);
		_destiny.OnUsed(shrine, user, destinyClaimsPerWave);
	}

	public void OnWaveCleared(int wave, Cadence ascensionResetWaves)
	{
		_ascension.OnWaveCleared(wave, ascensionResetWaves);
		_destiny.OnWaveCleared(wave);
		_hatred.OnWaveCleared(wave);
		_luck.OnWaveCleared(wave);
	}

	public void ResetHatredUses()
	{
		_hatred.ResetUses();
	}

	public void Tick(double now, int clearedWaves, Cadence ascensionResetWaves, bool cursesOnly)
	{
		if (!(now < _nextPinAt))
		{
			_nextPinAt = now + 1.0;
			_maw.PinUses();
			_ascension.LockUntilFirstReset(clearedWaves, ascensionResetWaves);
			if (cursesOnly)
			{
				_hatred.RequireCurses();
			}
		}
	}

	public void OnWaveStarted()
	{
		_maw.PinUses();
	}

	public void Forget()
	{
		_nextPinAt = 0.0;
		_well.Forget();
		_altar.Forget();
		_ascension.Forget();
		_maw.Forget();
		_destiny.Forget();
		_hatred.Forget();
		_luck.Forget();
	}
}
