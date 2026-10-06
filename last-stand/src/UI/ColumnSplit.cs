using System;
using System.Collections.Generic;

namespace LastStand.UI;

internal static class ColumnSplit
{
	public static int[] Evenly(IReadOnlyList<float> heights, int count)
	{
		int[] best = new int[count];
		int[] current = new int[count];
		float bestTallest = float.MaxValue;
		Search(heights, count, 0, 0, current, best, ref bestTallest);
		return best;
	}

	private static void Search(IReadOnlyList<float> heights, int count, int run, int start, int[] current, int[] best,
		ref float bestTallest)
	{
		int n = heights.Count;
		current[run] = start;
		if (run == count - 1)
		{
			float tallest = Tallest(heights, count, current);
			if (tallest < bestTallest)
			{
				bestTallest = tallest;
				current.CopyTo(best, 0);
			}

			return;
		}

		int last = Math.Max(start + 1, n - (count - run - 1));
		for (int next = start + 1; next <= last; next++)
		{
			Search(heights, count, run + 1, Math.Min(next, n), current, best, ref bestTallest);
		}
	}

	private static float Tallest(IReadOnlyList<float> heights, int count, int[] starts)
	{
		float tallest = 0f;
		for (int run = 0; run < count; run++)
		{
			int end = run + 1 < count ? starts[run + 1] : heights.Count;
			float sum = 0f;
			for (int i = starts[run]; i < end; i++)
			{
				sum += heights[i];
			}

			tallest = Math.Max(tallest, sum);
		}

		return tallest;
	}
}
