using LastStand.Config;
using LastStand.Text;
using Mirror;
using UnityEngine;

namespace LastStand.Game;

internal sealed class ArenaMusic
{
	private const int Unknown = -1;

	private FxNetworkedEffect _start;
	private bool _searched;
	private int _sentConnections = Unknown;
	private int _pendingConnections = Unknown;
	private double _sendAt;
	private double _nextCheckAt;

	public void Tick(double now, bool enabled)
	{
		if (!enabled || !NetworkServer.active || now < _nextCheckAt)
		{
			return;
		}

		_nextCheckAt = now + Tuning.ArenaMusicCheckSeconds;
		if (!_searched)
		{
			_searched = true;
			_start = FindBossMusic();
			if (_start == null)
			{
				Log.Info(LogText.ArenaMusicMissing);
				return;
			}
		}

		if (_start == null)
		{
			return;
		}

		int connections = PlayerRoster.ReadyConnections();
		if (connections != _pendingConnections)
		{
			_pendingConnections = connections;
			_sendAt = now + Tuning.ArenaMusicSendDelaySeconds;
		}

		if (_pendingConnections == _sentConnections || now < _sendAt)
		{
			return;
		}

		_start.PlayNetworked();
		_sentConnections = _pendingConnections;
		Log.Info(LogText.ArenaMusicPlayed(_start.name, _sentConnections));
	}

	public void Reset()
	{
		_start = null;
		_searched = false;
		_sentConnections = Unknown;
		_pendingConnections = Unknown;
		_sendAt = 0;
		_nextCheckAt = 0;
	}

	private static FxNetworkedEffect FindBossMusic()
	{
		foreach (FxNetworkedEffect effect in Object.FindObjectsByType<FxNetworkedEffect>(FindObjectsInactive.Include,
			FindObjectsSortMode.None))
		{
			if (effect.netId != 0 && effect.TryGetComponent(out FxChangeMusic change) && change.music != null)
			{
				return effect;
			}
		}

		return null;
	}
}
