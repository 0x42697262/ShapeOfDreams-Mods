using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;

namespace LastStand.Game;

internal sealed class RunGoal
{
	private double _nextCheckAt;

	public void Tick(double now)
	{
		if (now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.GoalCheckSeconds;

		QuestManager quests = NetworkedManagerBase<QuestManager>.softInstance;
		if (quests != null && quests.TryGetQuest(out Quest_ShapeOfDreams goal) && !goal.IsNullOrInactive())
		{
			goal.Destroy();
			Log.Info(LogText.GoalRemoved);
		}
	}

	public void Reset()
	{
		_nextCheckAt = 0;
	}
}
