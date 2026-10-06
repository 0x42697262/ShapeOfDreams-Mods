using System.Collections.Generic;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Arena;

internal sealed class ArenaMusic
{
	private readonly List<DewMusicItem> _choices = new List<DewMusicItem>();
	private DewMusicItem _last;

	public void OnRoomStarted(Room room, bool enabled)
	{
		if (!enabled || room == null || !ArenaLocator.IsInArenaNode())
		{
			return;
		}

		_choices.Clear();
		foreach (string guid in GameNames.BossMusicTrackGuids)
		{
			DewMusicItem item = MusicTracks.Find(guid);
			if (item != null && item != _last)
			{
				_choices.Add(item);
			}
		}

		if (_choices.Count == 0 && _last != null)
		{
			_choices.Add(_last);
		}

		if (_choices.Count == 0)
		{
			Log.Warn(LogText.BossMusicMissing);
			return;
		}

		DewMusicItem track = _choices[UnityEngine.Random.Range(0, _choices.Count)];
		_last = track;
		MusicTracks.Play(track);
		Log.Info(LogText.BossMusicPlaying(track.name));
	}
}
