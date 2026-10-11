using System;
using System.Globalization;
using LastStand.Config;
using LastStand.Waves;

namespace LastStand.Session;

internal static class ProgressStore
{
	private const char Separator = '|';

	private const int FieldCount = 6;

	private const int OldestFieldCount = 4;

	private const int FormatField = 4;

	private const int PaceField = 5;

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

	public static void Save(int node, int wave, bool inProgress, PaceLedger pace)
	{
		string run = CurrentRunKey();
		if (run == null)
		{
			return;
		}

		string data = string.Join(Separator.ToString(), run, node.ToString(CultureInfo.InvariantCulture),
			wave.ToString(CultureInfo.InvariantCulture),
			inProgress ? Tuning.ProgressInProgressFlag : Tuning.ProgressBetweenWavesFlag,
			ModIdentity.ProgressPaceFormat, pace.Serialize());
		AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, data);
	}

	public static bool TryLoad(int node, out int wave, out bool inProgress, out PaceLedger pace)
	{
		wave = 0;
		inProgress = false;
		pace = new PaceLedger();
		string run = CurrentRunKey();
		if (run == null || !(AppDomain.CurrentDomain.GetData(ModIdentity.ProgressSlot) is string data))
		{
			return false;
		}

		string[] parts = data.Split(Separator);
		if (parts.Length < OldestFieldCount || parts.Length > FieldCount || parts[0] != run
		    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int savedNode)
		    || savedNode != node
		    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out wave)
		    || wave <= 0)
		{
			wave = 0;
			return false;
		}

		inProgress = parts[3] == Tuning.ProgressInProgressFlag;
		if (parts.Length == FieldCount && parts[FormatField] == ModIdentity.ProgressPaceFormat)
		{
			pace = PaceLedger.Parse(parts[PaceField]);
		}

		return true;
	}

	public static void Forget()
	{
		AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, null);
	}
}
