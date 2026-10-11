using LastStand.Config;
using LastStand.Session;

namespace LastStand;

public class LastStandMod : ModEntry
{
	public LastStandConfig config = new LastStandConfig();

	internal static LastStandMod Current => Active as LastStandMod;

	internal LastStandSession Session { get; private set; }

	internal override ModConfig Config => config;

	protected override int ConfigSchemaVersion => ConfigSchema.Current;

	protected override void OnLoaded()
	{
		Session = new LastStandSession(SessionSettings.From(config));
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
		Session?.Reset();
	}

	protected override void OnWorldGenerated()
	{
		Session?.ApplyArenaRoom();
	}

	protected override bool SkipCutscenes()
	{
		return ArenaLocator.IsInArenaNode();
	}

	protected override bool SkipTravelVote()
	{
		return true;
	}
}
