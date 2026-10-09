using System;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Text;
using Mirror;

namespace LastStand.Game;

internal sealed class WaveTracker
{
	private const int Unknown = -1;

	private readonly ErrorThrottle _errors = new ErrorThrottle(Tuning.ErrorLogIntervalSeconds);
	private Quest_Limbo _quest;
	private string _shown;
	private int _labeledConnections = Unknown;
	private double _nextRefreshAt;

	public void Tick(double now, string progress)
	{
		if (now < _nextRefreshAt)
		{
			return;
		}

		_nextRefreshAt = now + Tuning.TrackerRefreshSeconds;
		try
		{
			Show(progress);
		}
		catch (Exception e)
		{
			_errors.Report(now, LogText.TrackerFailed, e);
		}
	}

	public void Remove()
	{
		if (!_quest.IsNullOrInactive() && NetworkServer.active)
		{
			_quest.Destroy();
			Log.Info(LogText.TrackerRemoved);
		}

		_quest = null;
		_shown = null;
		_labeledConnections = Unknown;
		_nextRefreshAt = 0;
	}

	private void Show(string progress)
	{
		if (progress == null)
		{
			Remove();
			return;
		}

		if (_quest.IsNullOrInactive() && !TryAttach(progress))
		{
			return;
		}

		if (progress != _shown)
		{
			_quest.currentProgress = progress;
			_shown = progress;
		}

		int connections = ReadyConnections();
		if (connections != _labeledConnections)
		{
			_quest.SetLocalizedTitle(GameNames.TrackerTitleKey);
			_quest.SetLocalizedDescription(GameNames.TrackerDescriptionKey);
			_labeledConnections = connections;
			Log.Info(LogText.TrackerLabelSent(connections));
		}
	}

	private bool TryAttach(string progress)
	{
		QuestManager quests = NetworkedManagerBase<QuestManager>.softInstance;
		if (quests == null)
		{
			return false;
		}

		_labeledConnections = Unknown;
		_shown = progress;
		if (quests.TryGetQuest(out Quest_Limbo existing) && !existing.IsNullOrInactive())
		{
			_quest = existing;
			_quest.progressType = QuestProgressType.Kills;
			_quest.currentProgress = progress;
			Log.Info(LogText.TrackerAdopted);
			return true;
		}

		_quest = quests.StartQuest<Quest_Limbo>(quest =>
		{
			quest.progressType = QuestProgressType.Kills;
			quest.currentProgress = progress;
		});
		if (_quest.IsNullOrInactive())
		{
			_quest = null;
			return false;
		}

		Log.Info(LogText.TrackerStarted);
		return true;
	}

	private static int ReadyConnections()
	{
		int ready = 0;
		foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
		{
			if (connection != null && connection.isReady)
			{
				ready++;
			}
		}

		return ready;
	}
}
