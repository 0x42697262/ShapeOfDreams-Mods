using System.Collections.Generic;
using LastStand.Config;
using LastStand.Text;
using UnityEngine;
using UnityEngine.AI;

namespace LastStand.Arena;

internal static class ArmTipPlacement
{
	private static readonly List<Vector3> Tips = new List<Vector3>();

	public static bool TryPick(int count, out Vector3[] spots)
	{
		spots = null;
		if (count <= 0 || !SpawnAnchors.TryFill(Tips, out Vector3 center) || Tips.Count < count)
		{
			return false;
		}

		Tips.Sort((a, b) => Angle(center, a).CompareTo(Angle(center, b)));
		spots = new Vector3[count];
		float pastTips = 0f;
		for (int i = 0; i < count; i++)
		{
			Vector3 tip = Tips[i * Tips.Count / count];
			spots[i] = ArmEdge(center, tip);
			pastTips += Flat(center, spots[i]) - Flat(center, tip);
		}

		Log.Info(LogText.ArmTipRing(count, Tips.Count, SpawnAnchors.Source, pastTips / count));
		return true;
	}

	private static Vector3 ArmEdge(Vector3 center, Vector3 tip)
	{
		Vector3 outward = tip - center;
		outward.y = 0f;
		outward.Normalize();

		float reach = 0f;
		for (float distance = Tuning.ArmEdgeStepMeters; distance <= Tuning.ArmEdgeSearchMeters;
		     distance += Tuning.ArmEdgeStepMeters)
		{
			Vector3 probe = tip + outward * distance;
			if (!NavMesh.SamplePosition(probe, out _, Tuning.EdgeSampleRadius, NavMesh.AllAreas) ||
			    !PlayableGround.IsPlayable(probe))
			{
				break;
			}

			reach = distance;
		}

		float edge = Mathf.Max(reach - Tuning.EdgeInset, 0f);
		return PlayableGround.PullToward(tip, tip + outward * edge);
	}

	private static float Angle(Vector3 center, Vector3 point)
	{
		return Mathf.Atan2(point.z - center.z, point.x - center.x);
	}

	private static float Flat(Vector3 center, Vector3 point)
	{
		return Vector2.Distance(point.ToXY(), center.ToXY());
	}
}
