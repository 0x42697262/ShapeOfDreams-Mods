using System;
using System.Globalization;

namespace LastStand.Session;

internal static class ProgressStore
{
	private const string Slot = "LastStand.Progress";
	private const char Separator = '|';
	private const int FieldCount = 4;
	private const string InProgressFlag = "1";
	private const string BetweenWavesFlag = "0";

	public static string CurrentRunKey()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (game == null || zones == null)
		{
			return null;
		}

		return game.GetInstanceID().ToString(CultureInfo.InvariantCulture) + ":" +
		       zones.worldSeed.ToString(CultureInfo.InvariantCulture);
	}

	public static void Save(int node, int wave, bool inProgress)
	{
		string run = CurrentRunKey();
		if (run == null)
		{
			return;
		}

		string data = string.Join(Separator.ToString(), run, node.ToString(CultureInfo.InvariantCulture),
			wave.ToString(CultureInfo.InvariantCulture), inProgress ? InProgressFlag : BetweenWavesFlag);
		AppDomain.CurrentDomain.SetData(Slot, data);
	}

	public static bool TryLoad(int node, out int wave, out bool inProgress)
	{
		wave = 0;
		inProgress = false;
		string run = CurrentRunKey();
		if (run == null || !(AppDomain.CurrentDomain.GetData(Slot) is string data))
		{
			return false;
		}

		string[] parts = data.Split(Separator);
		if (parts.Length != FieldCount || parts[0] != run
		    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int savedNode)
		    || savedNode != node
		    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out wave)
		    || wave <= 0)
		{
			wave = 0;
			return false;
		}

		inProgress = parts[3] == InProgressFlag;
		return true;
	}

	public static void Forget()
	{
		AppDomain.CurrentDomain.SetData(Slot, null);
	}
}
