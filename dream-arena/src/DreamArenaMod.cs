using System;
using System.Reflection;
using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Session;
using DreamArena.Text;
using DreamArena.UI;
using UnityEngine;

namespace DreamArena;

public class DreamArenaMod : ModBehaviour
{
	public DreamArenaConfig config = new DreamArenaConfig();

	internal static DreamArenaMod Current { get; private set; }

	internal DreamArenaSession Session { get; private set; }

	private readonly ErrorThrottle _tickErrors = new ErrorThrottle(Tuning.ErrorLogIntervalSeconds);

	private readonly SettingsAnnouncer _settingsAnnouncer = new SettingsAnnouncer();

	private void Awake()
	{
		Current = this;
	}

	private void Start()
	{
		instance.isAlteringGameplay = true;
		Session = new DreamArenaSession();
		Session.UseSettings(SessionSettings.From(config));
		ChoiceFields.Register();
		harmony.PatchAll();
		CallOnNetworkedManager<GameManager>(OnGameManagerStarted);
		Log.Info(LogText.Loaded(mod.metadata.name, mod.metadata.modVer));
	}

	private void Update()
	{
		if (Session == null)
		{
			return;
		}

		try
		{
			Session.Tick(Time.time);
		}
		catch (Exception e)
		{
			_tickErrors.Report(Time.time, LogText.TickFailed, e);
		}
	}

	private void OnDestroy()
	{
		Session?.Shutdown();
		ConfigWindow.Close();
		InjectedUi.RemoveAll();
		LobbyModeOption.Detach();
		ChoiceFields.Unregister();
		harmony.UnpatchAll(harmony.Id);

		if (Current == this)
		{
			Current = null;
		}

		Log.Info(LogText.Unloaded);
	}

	public override void LoadConfigsToDisk()
	{
		foreach (FieldInfo field in modConfigFields)
		{
			ConfigFile.ResetIfOutdated(GetModConfigFilePath(field));
		}

		base.LoadConfigsToDisk();
		_settingsAnnouncer.Remember(config);
	}

	internal void ApplyConfig(DreamArenaConfig edited)
	{
		edited.CopyTo(config);
		try
		{
			SaveConfigsToDisk();
			Log.Info(LogText.ConfigApplied);
		}
		catch (Exception e)
		{
			Log.Error(LogText.ConfigSaveFailed, e);
		}

		OnConfigChanged();
	}

	public override void OnConfigChanged()
	{
		_settingsAnnouncer.Announce(config);
		Session?.UseSettings(SessionSettings.From(config));
	}

	private void OnGameManagerStarted(GameManager game)
	{
		Session?.Reset();
		Session?.UseSettings(SessionSettings.From(config));
	}
}
