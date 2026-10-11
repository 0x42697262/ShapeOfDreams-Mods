using DreamArena.Config;
using DreamArena.Session;

namespace DreamArena;

public class DreamArenaMod : ModEntry
{
	public DreamArenaConfig config = new DreamArenaConfig();

	internal static DreamArenaMod Current => Active as DreamArenaMod;

	internal DreamArenaSession Session { get; private set; }

	internal override ModConfig Config => config;

	protected override int ConfigSchemaVersion => ConfigSchema.Current;

	protected override void OnLoaded()
	{
		Session = new DreamArenaSession();
		Session.UseSettings(SessionSettings.From(config));
	}

	protected override void OnTick(double now)
	{
		Session?.Tick(now);
	}

	protected override void OnSettingsChanged()
	{
		Session?.UseSettings(SessionSettings.From(config));
	}

	protected override void OnGameStarted()
	{
		Session?.Reset();
		Session?.UseSettings(SessionSettings.From(config));
	}

	protected override void OnUnloading()
	{
		Session?.Shutdown();
	}
}
