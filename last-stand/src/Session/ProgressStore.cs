using System;
using System.Globalization;
using LastStand.Config;

namespace LastStand.Session;

internal static class ProgressStore
{
	private const char Separator = '|';

	private const int FieldCount = 6;

	private const int FieldCountWithoutHeroicKills = 5;

	private const int FieldCountWithoutFastClears = 4;

	private const double NoHeroicKills = 1;

	public static string CurrentRunKey()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (game == null || zones == null)
		{
			return null;
		}

		return game.GetInstanceID().ToString(CultureInfo.InvariantCulture) + Tuning.RunKeySeparator +
		       zones.worldSeed.ToString(CultureInfo.InvariantCulture);
	}

	public static void Save(int node, int wave, bool inProgress, int fastClears, double heroicKillMultiplier)
	{
		string run = CurrentRunKey();
		if (run == null)
		{
			return;
		}

		string data = string.Join(Separator.ToString(), run, node.ToString(CultureInfo.InvariantCulture),
			wave.ToString(CultureInfo.InvariantCulture),
			inProgress ? Tuning.ProgressInProgressFlag : Tuning.ProgressBetweenWavesFlag,
			fastClears.ToString(CultureInfo.InvariantCulture),
			heroicKillMultiplier.ToString(CultureInfo.InvariantCulture));
		AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, data);
	}

	public static bool TryLoad(int node, out int wave, out bool inProgress, out int fastClears,
		out double heroicKillMultiplier)
	{
		wave = 0;
		inProgress = false;
		fastClears = 0;
		heroicKillMultiplier = NoHeroicKills;
		string run = CurrentRunKey();
		if (run == null || !(AppDomain.CurrentDomain.GetData(ModIdentity.ProgressSlot) is string data))
		{
			return false;
		}

		string[] parts = data.Split(Separator);
		if ((parts.Length != FieldCount && parts.Length != FieldCountWithoutHeroicKills &&
		     parts.Length != FieldCountWithoutFastClears) || parts[0] != run
		    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int savedNode)
		    || savedNode != node
		    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out wave)
		    || wave <= 0)
		{
			wave = 0;
			return false;
		}

		inProgress = parts[3] == Tuning.ProgressInProgressFlag;
		if (parts.Length >= FieldCountWithoutHeroicKills)
		{
			int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out fastClears);
		}

		if (parts.Length == FieldCount &&
		    !double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out heroicKillMultiplier))
		{
			heroicKillMultiplier = NoHeroicKills;
		}

		return true;
	}

	public static void Forget()
	{
		AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, null);
	}
}
