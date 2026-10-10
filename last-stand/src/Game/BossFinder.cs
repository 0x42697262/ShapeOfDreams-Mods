using LastStand.Arena;
using LastStand.Diagnostics;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Game;

internal static class BossFinder
{
	private const float SectorDegrees = 45f;

	public static void LogAll()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		Hero host = DewPlayer.local != null ? DewPlayer.local.hero : null;
		Vector3 from = host != null ? host.agentPosition : Vector3.zero;
		int found = 0;
		foreach (Entity entity in actors.allEntities)
		{
			if (!(entity is BossMonster boss) || boss.IsNullInactiveDeadOrKnockedOut())
			{
				continue;
			}

			found++;
			Vector3 position = boss.agentPosition;
			Log.Info(LogText.BossFound(boss.GetType().Name, position,
				Vector2.Distance(from.ToXY(), position.ToXY()), ScreenDirection(from, position),
				boss.Status.TryGetStatusEffect(out Se_PureDream_Statue _), !PlayableGround.IsPlayable(position),
				boss.normalizedHealth));
		}

		Log.Info(LogText.BossesFound(found));
	}

	private static string ScreenDirection(Vector3 from, Vector3 to)
	{
		Camera camera = Camera.main;
		Vector3 forward = camera != null ? camera.transform.forward : Vector3.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 right = Vector3.Cross(Vector3.up, forward);
		Vector3 offset = to - from;
		float angle = Mathf.Atan2(Vector3.Dot(offset, right), Vector3.Dot(offset, forward)) * Mathf.Rad2Deg;
		int sectors = LogText.ScreenDirections.Length;
		int index = ((Mathf.RoundToInt(angle / SectorDegrees) % sectors) + sectors) % sectors;
		return LogText.ScreenDirections[index];
	}
}
