using LastStand.Config;
using UnityEngine;

namespace LastStand.Arena;

internal static class PlayableGround
{
	public static bool IsPlayable(Vector3 position)
	{
		return !Dew.IsNotPlayableOnTop(position);
	}

	public static Vector3 PullToward(Vector3 safe, Vector3 spot)
	{
		Vector3 offset = spot - safe;
		float length = offset.magnitude;
		if (length <= 0f)
		{
			return safe;
		}

		Vector3 direction = offset / length;
		for (float distance = length; distance > 0f; distance -= Tuning.PlayableStepMeters)
		{
			Vector3 candidate = Dew.GetValidAgentDestination_LinearSweep(safe, safe + direction * distance);
			if (IsPlayable(candidate))
			{
				return candidate;
			}
		}

		return safe;
	}
}
