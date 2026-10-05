using LastStand.Diagnostics;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class DreamWell
{
	private const int Unlimited = -1;
	private const float NoCooldown = 0f;

	private Shrine_UpgradeWell _well;

	public void Place(Vector3 position)
	{
		if (_well != null)
		{
			return;
		}

		_well = Dew.CreateActor<Shrine_UpgradeWell>(position, null, null, Configure);
		Log.Info(LogText.WellPlaced(position));
	}

	public void OnUsed(Shrine shrine)
	{
		if (shrine == null || shrine != _well)
		{
			return;
		}

		shrine.ResetUseCounts();
		shrine.Network_lastUseTime = double.NegativeInfinity;
		shrine.MakeAvailable();
	}

	public void Forget()
	{
		_well = null;
	}

	private static void Configure(Shrine_UpgradeWell well)
	{
		well.maxUseCountPerPlayer = Unlimited;
		well.NetworkmaxTotalUseCount = Unlimited;
		well.useCooldown = NoCooldown;
	}
}
