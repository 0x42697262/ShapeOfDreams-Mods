using LastStand.Diagnostics;
using LastStand.Mode;
using LastStand.Text;

namespace LastStand;

public class LastStandConsole : ModBehaviour
{
	[ConsoleCommand(CommandText.StatusDescription, CommandText.Status)]
	public void Cmd_Status()
	{
		LastStandMod mod = LastStandMod.Current;
		if (mod?.Session != null)
		{
			Log.Info(mod.Session.Describe());
		}
	}

	[ConsoleCommand(CommandText.NextWaveDescription, CommandText.NextWave)]
	public void Cmd_NextWave()
	{
		if (!LastStandMode.IsHosting())
		{
			Log.Warn(LogText.NotHosting);
			return;
		}

		LastStandMod.Current?.Session?.StartNextWaveNow();
	}

	[ConsoleCommand(CommandText.SoloDescription, CommandText.Solo)]
	public void Cmd_Solo()
	{
		ModeSelection.StartSolo();
	}
}
