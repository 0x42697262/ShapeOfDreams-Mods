using System;
using System.Reflection;
using LastStand.Config;
using LastStand.Diagnostics;
using LastStand.Session;
using LastStand.Text;
using LastStand.UI;
using UnityEngine;

namespace LastStand;

public class LastStandMod : ModBehaviour
{
	public LastStandConfig config = new LastStandConfig();

	internal static LastStandMod Current { get; private set; }

	internal LastStandSession Session { get; private set; }

	internal LocationLabel Hud { get; } = new LocationLabel();

	private Func<double, string> _hudLabel;

	private void Awake()
	{
		Current = this;
	}

	private void Start()
	{
		instance.isAlteringGameplay = true;
		Session = new LastStandSession(SessionSettings.From(config));
		_hudLabel = Session.HudLabel;
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
			Log.Error(LogText.TickFailed, e);
			Session.Reset();
		}

		try
		{
			Hud.Tick(Time.time, _hudLabel);
		}
		catch (Exception e)
		{
			Log.Error(LogText.HudFailed, e);
			Hud.Show(null);
		}
	}

	private void OnDestroy()
	{
		Session?.Reset();
		Hud.Show(null);
		ConfigWindow.Close();
		InjectedUi.RemoveAll();
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
	}

	internal void ApplyConfig(LastStandConfig edited)
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
		Session?.UseSettings(SessionSettings.From(config));
	}

	private void OnGameManagerStarted(GameManager game)
	{
		Session?.Reset();
		Session?.UseSettings(SessionSettings.From(config));
	}
}
