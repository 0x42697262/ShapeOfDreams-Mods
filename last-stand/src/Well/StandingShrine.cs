using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class StandingShrine<T> where T : Shrine
{
	private const int Unlimited = -1;
	private const float NoCooldown = 0f;

	private readonly string _name;
	private T _shrine;

	public StandingShrine(string name)
	{
		_name = name;
	}

	public void Place(Vector3 position)
	{
		if (_shrine != null)
		{
			return;
		}

		_shrine = FindExisting(position);
		if (_shrine != null)
		{
			Configure(_shrine);
			Log.Info(LogText.ShrineReused(_name, position));
			return;
		}

		_shrine = Dew.CreateActor<T>(position, null, null, Configure);
		Log.Info(LogText.ShrinePlaced(_name, position));
	}

	public void OnUsed(Shrine shrine)
	{
		if (shrine == null || shrine != _shrine)
		{
			return;
		}

		shrine.ResetUseCounts();
		shrine.Network_lastUseTime = double.NegativeInfinity;
		shrine.MakeAvailable();
	}

	public void Forget()
	{
		_shrine = null;
	}

	private static T FindExisting(Vector3 position)
	{
		foreach (T shrine in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
		{
			if (!shrine.IsNullOrInactive()
			    && Vector3.Distance(shrine.transform.position, position) <= Tuning.ShrineAdoptRadius)
			{
				return shrine;
			}
		}

		return null;
	}

	private static void Configure(T shrine)
	{
		shrine.maxUseCountPerPlayer = Unlimited;
		shrine.NetworkmaxTotalUseCount = Unlimited;
		shrine.useCooldown = NoCooldown;
	}
}
