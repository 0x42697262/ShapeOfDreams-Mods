using LastStand.Diagnostics;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class StandingShrine<T> where T : Shrine
{
	private const int Unlimited = -1;
	private const float NoCooldown = 0f;

	private readonly string _name;
	private readonly ShrineUses _uses;
	private readonly float _adoptRadius;
	private T _shrine;

	public StandingShrine(string name, ShrineUses uses, float adoptRadius)
	{
		_name = name;
		_uses = uses;
		_adoptRadius = adoptRadius;
	}

	public T Current => _shrine;

	public bool Owns(Shrine shrine)
	{
		return shrine != null && shrine == _shrine;
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
		if (_uses != ShrineUses.Unlimited || !Owns(shrine))
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

	private T FindExisting(Vector3 position)
	{
		foreach (T shrine in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
		{
			if (!shrine.IsNullOrInactive()
			    && Vector3.Distance(shrine.transform.position, position) <= _adoptRadius)
			{
				return shrine;
			}
		}

		return null;
	}

	private void Configure(T shrine)
	{
		if (_uses == ShrineUses.Unlimited)
		{
			shrine.maxUseCountPerPlayer = Unlimited;
			shrine.NetworkmaxTotalUseCount = Unlimited;
			shrine.useCooldown = NoCooldown;
			return;
		}

		T prefab = DewResources.GetByType<T>();
		if (prefab != null)
		{
			shrine.maxUseCountPerPlayer = prefab.maxUseCountPerPlayer;
			shrine.NetworkmaxTotalUseCount = prefab.maxTotalUseCount;
			shrine.useCooldown = prefab.useCooldown;
		}
	}
}
