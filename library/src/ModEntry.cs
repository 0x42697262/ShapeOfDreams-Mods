using System;
using System.Reflection;
using UnityEngine;

namespace SodLib;

public abstract class ModEntry : ModBehaviour
{
	private readonly ErrorThrottle _tickErrors = new ErrorThrottle(SharedTuning.ErrorLogIntervalSeconds);
	private readonly SettingsAnnouncer _settingsAnnouncer = new SettingsAnnouncer();

	internal static ModEntry Active { get; private set; }

	internal abstract ModConfig Config { get; }

	protected abstract int ConfigSchemaVersion { get; }

	protected abstract void OnLoaded();

	protected abstract void OnTick(double now);

	protected abstract void OnSettingsChanged();

	protected abstract void OnGameStarted();

	protected abstract void OnUnloading();

	protected abstract void OnWorldGenerated();

	protected abstract bool SkipCutscenes();

	protected abstract bool SkipTravelVote();

	protected void Awake()
	{
		Active = this;
	}

	protected void Start()
	{
		instance.isAlteringGameplay = true;
		OnLoaded();
		ChoiceFields.Register();
		harmony.PatchAll();
		CallOnNetworkedManager<GameManager>(_ => OnGameStarted());
		Log.Info(SharedLogText.Loaded(mod.metadata.name, mod.metadata.modVer));
	}

	protected void Update()
	{
		try
		{
			OnTick(Time.time);
		}
		catch (Exception e)
		{
			_tickErrors.Report(Time.time, ModText.TickFailed, e);
		}
	}

	protected void OnDestroy()
	{
		OnUnloading();
		ConfigWindow.Close();
		InjectedUi.RemoveAll();
		LobbyModeOption.Detach();
		ChoiceFields.Unregister();
		harmony.UnpatchAll(harmony.Id);
		if (Active == this)
		{
			Active = null;
		}

		Log.Info(SharedLogText.Unloaded);
	}

	public override void LoadConfigsToDisk()
	{
		foreach (FieldInfo field in modConfigFields)
		{
			ConfigFile.ResetIfOutdated(GetModConfigFilePath(field), ConfigSchemaVersion);
		}

		base.LoadConfigsToDisk();
		_settingsAnnouncer.Remember(Config);
	}

	internal bool SkipsTravelVote()
	{
		try
		{
			return SkipTravelVote();
		}
		catch (Exception e)
		{
			_tickErrors.Report(Time.time, SharedLogText.TravelVoteCheckFailed, e);
			return false;
		}
	}

	internal bool SkipsCutscene()
	{
		try
		{
			return SkipCutscenes();
		}
		catch (Exception e)
		{
			_tickErrors.Report(Time.time, SharedLogText.CutsceneCheckFailed, e);
			return false;
		}
	}

	internal void WorldGenerated()
	{
		try
		{
			OnWorldGenerated();
		}
		catch (Exception e)
		{
			Log.Error(SharedLogText.WorldGenFailed, e);
		}
	}

	internal void ApplyConfig(ModConfig edited)
	{
		edited.CopyTo(Config);
		try
		{
			SaveConfigsToDisk();
			Log.Info(SharedLogText.ConfigApplied);
		}
		catch (Exception e)
		{
			Log.Error(SharedLogText.ConfigSaveFailed, e);
		}

		OnConfigChanged();
	}

	public override void OnConfigChanged()
	{
		_settingsAnnouncer.Announce(Config);
		OnSettingsChanged();
	}
}
