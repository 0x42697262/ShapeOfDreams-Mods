using System.Collections.Generic;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class DestinyShrine
{
	private const int ClaimsLeftOpen = 0;

	private readonly StandingShrine<Shrine_Destiny> _standing =
		new StandingShrine<Shrine_Destiny>(LogText.ShrineOfDestinyName, ShrineUses.Vanilla, float.PositiveInfinity);

	private readonly Dictionary<string, int> _claims = new Dictionary<string, int>();

	public void Place(Vector3 position)
	{
		_standing.Place(position);
	}

	public void OnUsed(Shrine shrine, Entity user, int claimsPerWave)
	{
		DewPlayer player = user != null ? user.owner : null;
		if (!_standing.Owns(shrine) || player == null || player.guid == null)
		{
			return;
		}

		_claims.TryGetValue(player.guid, out int claims);
		claims++;
		_claims[player.guid] = claims;
		if (claims >= claimsPerWave)
		{
			return;
		}

		UseCountPin.Set(shrine, player, ClaimsLeftOpen);
		shrine.MakeAvailable();
	}

	public void OnWaveCleared(int wave)
	{
		_claims.Clear();
		Shrine_Destiny shrine = _standing.Current;
		if (shrine.IsNullOrInactive())
		{
			return;
		}

		shrine.ResetUseCounts();
		Log.Info(LogText.DestinyClaimsReset(wave));
	}

	public void Forget()
	{
		_claims.Clear();
		_standing.Forget();
	}
}
