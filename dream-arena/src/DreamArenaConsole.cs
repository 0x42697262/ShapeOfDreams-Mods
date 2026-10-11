using DreamArena.Diagnostics;
using DreamArena.Heroes;
using DreamArena.Mode;
using DreamArena.Text;
using DreamArena.UI;
using Mirror;

namespace DreamArena;

public class DreamArenaConsole : ModBehaviour
{
	[ConsoleCommand(CommandText.ConfigDescription, CommandText.Config)]
	public void Cmd_Config()
	{
		if (NetworkClient.active && !NetworkServer.active)
		{
			Log.Warn(LogText.ConfigHostOnly);
			return;
		}

		ConfigWindow.Toggle();
	}

	[ConsoleCommand(CommandText.StatusDescription, CommandText.Status)]
	public void Cmd_Status()
	{
		Session.DreamArenaSession session = DreamArenaMod.Current?.Session;
		if (session?.Settings != null)
		{
			Log.Info(session.Describe());
		}
	}

	[ConsoleCommand(CommandText.StartDescription, CommandText.Start)]
	public void Cmd_Start()
	{
		if (Hosting())
		{
			DreamArenaMod.Current?.Session?.StageNow();
		}
	}

	[ConsoleCommand(CommandText.RestartDescription, CommandText.Restart)]
	public void Cmd_Restart()
	{
		if (!Hosting())
		{
			return;
		}

		string reason = DreamArenaMod.Current?.Session?.RestartNow();
		if (reason != null)
		{
			Log.Warn(reason);
		}
	}

	[ConsoleCommand(CommandText.ReviveDescription, CommandText.Revive)]
	public void Cmd_Revive()
	{
		if (Hosting())
		{
			Log.Info(LogText.Revived(Revives.ReviveAll()));
		}
	}

	[ConsoleCommand(CommandText.ScanDescription, CommandText.Scan)]
	public void Cmd_Scan()
	{
		ArenaScan.Report();
	}

	[ConsoleCommand(CommandText.PurgeDescription, CommandText.Purge)]
	public void Cmd_Purge()
	{
		if (Hosting())
		{
			DreamArenaMod.Current?.Session?.PurgeNow();
		}
	}

	private static bool Hosting()
	{
		if (ArenaMode.IsHosting())
		{
			return true;
		}

		Log.Warn(LogText.NotHosting);
		return false;
	}
}
