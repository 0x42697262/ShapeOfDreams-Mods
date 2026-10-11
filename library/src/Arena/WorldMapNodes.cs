using UnityEngine;

namespace SodLib.Arena;

internal static class WorldMapNodes
{
	public static int HideAllBut(ZoneManager zones, int keep)
	{
		if (zones == null)
		{
			return 0;
		}

		int hidden = 0;
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			WorldNodeData node = zones.nodes[i];
			if (i == keep || i == zones.currentNodeIndex || node.type == WorldNodeType.Start || node.IsSidetrackNode())
			{
				continue;
			}

			node.position = new Vector2(SharedTuning.HiddenMapNodeOffset - node.position.x, node.position.y);
			zones.nodes[i] = node;
			hidden++;
		}

		if (hidden > 0)
		{
			Log.Info(SharedLogText.MapNodesHidden(hidden, keep));
		}

		return hidden;
	}
}
