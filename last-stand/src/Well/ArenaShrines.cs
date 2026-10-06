using LastStand.Arena;
using LastStand.Config;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class ArenaShrines
{
	private readonly StandingShrine<Shrine_UpgradeWell> _well =
		new StandingShrine<Shrine_UpgradeWell>(LogText.DreamWellName);

	private readonly StandingShrine<Shrine_AltarOfCleansing> _altar =
		new StandingShrine<Shrine_AltarOfCleansing>(LogText.AltarOfCleansingName);

	public void Place(ArenaPoints arena, bool altarOfCleansing)
	{
		_well.Place(arena.Center);

		if (altarOfCleansing)
		{
			Vector3 wanted = arena.Center + Vector3.right * Tuning.AltarDistanceFromWell;
			_altar.Place(Dew.GetValidAgentDestination_LinearSweep(arena.Center, wanted));
		}
	}

	public void OnUsed(Shrine shrine)
	{
		_well.OnUsed(shrine);
		_altar.OnUsed(shrine);
	}

	public void Forget()
	{
		_well.Forget();
		_altar.Forget();
	}
}
