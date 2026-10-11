using System.Collections.Generic;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Game;
using LastStand.Mode;
using LastStand.Text;

namespace LastStand.UI;

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

		bool broadcast = LastStandMode.IsHosting();
		for (int i = 0; i < changes.Count; i++)
		{
			string line = SettingChangeText.Describe(changes[i]);
			Log.Info(LogText.SettingChanged(line));
			if (broadcast && i < Tuning.MaxSettingChangesInChat)
			{
				ChatNotices.Broadcast(Messages.SettingChanged(line));
			}
		}

		int hidden = changes.Count - Tuning.MaxSettingChangesInChat;
		if (broadcast && hidden > 0)
		{
			ChatNotices.Broadcast(Messages.MoreSettingsChanged(hidden));
		}
	}
}
