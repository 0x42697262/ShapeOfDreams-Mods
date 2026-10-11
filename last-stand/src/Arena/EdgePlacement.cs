using System;
using System.Collections.Generic;
using LastStand.Config;
using LastStand.Text;
using UnityEngine;
using UnityEngine.AI;

namespace LastStand.Arena;

internal static class EdgePlacement
{
	private static readonly List<Vector3> Floor = new List<Vector3>();

	public static Vector3[] PickRing(ArenaPoints arena, int count)
	{
		if (ArmTipPlacement.TryPick(count, out Vector3[] tips))
		{
			return tips;
		}

		Vector3[] spots = new Vector3[count];
		if (count <= 0)
		{
			return spots;
		}

		float[] reaches = new float[Tuning.EdgeDirections];
		int blocked = 0;
		for (int d = 0; d < reaches.Length; d++)
		{
			reaches[d] = Reach(arena.Center, DirectionAt(d), out bool unplayable);
			if (unplayable)
			{
				blocked++;
			}
		}

		float cap = Median(reaches) * Tuning.EdgeMaxShareOfMedian;
		int start = BestStart(reaches, count, cap);
		for (int i = 0; i < count; i++)
		{
			int d = ShrineDirection(start, i, count);
			float radius = Mathf.Max(Mathf.Min(reaches[d], cap) - Tuning.EdgeInset, Tuning.EdgeFallbackRadius);
			spots[i] = PlayableGround.PullToward(arena.Center, arena.Center + DirectionAt(d) * radius);
		}

		Log.Info(LogText.EdgeRing(count, Median(reaches), cap, blocked));
		return spots;
	}


	private static int BestStart(float[] reaches, int count, float cap)
	{
		int best = 0;
		float bestTotal = float.MinValue;
		for (int start = 0; start < reaches.Length; start++)
		{
			float total = 0f;
			for (int i = 0; i < count; i++)
			{
				total += Mathf.Min(reaches[ShrineDirection(start, i, count)], cap);
			}

			if (total > bestTotal)
			{
				bestTotal = total;
				best = start;
			}
		}

		return best;
	}

	private static int ShrineDirection(int start, int shrine, int count)
	{
		return (start + Mathf.RoundToInt((float)shrine * Tuning.EdgeDirections / count)) % Tuning.EdgeDirections;
	}

	private static float Median(float[] values)
	{
		float[] sorted = (float[])values.Clone();
		Array.Sort(sorted);
		return sorted[sorted.Length / 2];
	}

	private static float Reach(Vector3 center, Vector3 direction, out bool unplayable)
	{
		Floor.Clear();
		unplayable = false;
		for (float distance = Tuning.EdgeRayStartDistance; distance <= Tuning.EdgeSearchDistance;
		     distance += Tuning.EdgeMarchStep)
		{
			if (!NavMesh.SamplePosition(center + direction * distance, out NavMeshHit hit, Tuning.EdgeSampleRadius,
				    NavMesh.AllAreas))
			{
				continue;
			}

			if (!PlayableGround.IsPlayable(hit.position))
			{
				unplayable = true;
				break;
			}

			Floor.Add(hit.position);
		}

		int checks = 0;
		for (int i = Floor.Count - 1; i >= 0 && checks < Tuning.EdgePathChecks; i--, checks++)
		{
			if (PlayableGround.IsReachable(center, Floor[i]))
			{
				return Flat(center, Floor[i]);
			}
		}

		if (unplayable)
		{
			return Floor.Count > 0 ? Flat(center, Floor[0]) : 0f;
		}

		return RaycastReach(center, direction);
	}

	private static float RaycastReach(Vector3 center, Vector3 direction)
	{
		Vector3 start = center + direction * Tuning.EdgeRayStartDistance;
		if (!NavMesh.SamplePosition(start, out NavMeshHit from, Tuning.EdgeSampleRadius, NavMesh.AllAreas))
		{
			return 0f;
		}

		Vector3 target = from.position + direction * Tuning.EdgeSearchDistance;
		Vector3 boundary = NavMesh.Raycast(from.position, target, out NavMeshHit hit, NavMesh.AllAreas)
			? hit.position
			: target;
		return Flat(center, boundary);
	}

	private static float Flat(Vector3 center, Vector3 point)
	{
		return Vector2.Distance(point.ToXY(), center.ToXY());
	}

	private static Vector3 DirectionAt(int index)
	{
		return Quaternion.Euler(0f, index * SharedTuning.FullCircleDegrees / Tuning.EdgeDirections, 0f) * Vector3.forward;
	}
}
