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

	private void Awake()
	{
		Current = this;
	}

	private void Start()
	{
		instance.isAlteringGameplay = true;
		Session = new LastStandSession(SessionSettings.From(config));
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
	}

	private void OnDestroy()
	{
		Session?.Reset();
		InjectedUi.RemoveAll();
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
