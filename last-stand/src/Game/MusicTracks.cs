using System.Collections.Generic;

namespace LastStand.Game;

internal static class MusicTracks
{
	private static readonly Dictionary<string, DewMusicItem> Found = new Dictionary<string, DewMusicItem>();

	public static DewMusicItem Find(string guid)
	{
		if (Found.TryGetValue(guid, out DewMusicItem cached))
		{
			return cached;
		}

		DewMusicItem item = DewResources.GetByGuid<DewMusicItem>(guid);
		Found[guid] = item;
		return item;
	}

	public static void Play(DewMusicItem item)
	{
		MusicManager music = ManagerBase<MusicManager>.softInstance;
		if (music != null && item != null)
		{
			music.Play(item);
		}
	}
}
