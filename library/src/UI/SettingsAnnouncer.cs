using System.Collections.Generic;

namespace SodLib.UI;

internal sealed class SettingsAnnouncer
{
	private ModConfig _applied;

	public void Remember(ModConfig config)
	{
		_applied = config.Clone();
	}

	public void Announce(ModConfig config)
	{
		if (_applied == null)
		{
			Remember(config);
			return;
		}

		List<SettingChange> changes = SettingChange.Between(_applied, config);
		Remember(config);
		if (changes.Count == 0)
		{
			return;
		}

		bool broadcast = ModMode.IsHosting();
		for (int i = 0; i < changes.Count; i++)
		{
			string line = SettingChangeText.Describe(changes[i]);
			Log.Info(SharedLogText.SettingChanged(line));
			if (broadcast && i < SharedTuning.MaxSettingChangesInChat)
			{
				ChatNotices.Broadcast(SharedMessages.SettingChanged(line));
			}
		}

		int hidden = changes.Count - SharedTuning.MaxSettingChangesInChat;
		if (broadcast && hidden > 0)
		{
			ChatNotices.Broadcast(SharedMessages.MoreSettingsChanged(hidden));
		}
	}
}
