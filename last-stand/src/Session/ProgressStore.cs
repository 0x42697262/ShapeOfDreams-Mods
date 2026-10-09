using System.Globalization;
using System;

namespace LastStand.Session;

internal static class ProgressStore
{
	private const string Slot = "LastStand.Progress";

	private const char Separator = '|';

	private const int FieldCount = 5;

	private const int FieldCountWithoutFastClears = 4;

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

	public static void Save(int node, int wave, bool inProgress, int fastClears)
	{
		string text = CurrentRunKey();
		if (text != null)
		{
			string data = string.Join('|'.ToString(), text, node.ToString(CultureInfo.InvariantCulture), wave.ToString(CultureInfo.InvariantCulture), inProgress ? "1" : "0", fastClears.ToString(CultureInfo.InvariantCulture));
			AppDomain.CurrentDomain.SetData("LastStand.Progress", data);
		}
	}

	public static bool TryLoad(int node, out int wave, out bool inProgress, out int fastClears)
	{
		wave = 0;
		inProgress = false;
		fastClears = 0;
		string text = CurrentRunKey();
		if (text == null || !(AppDomain.CurrentDomain.GetData("LastStand.Progress") is string text2))
		{
			return false;
		}
		string[] array = text2.Split('|');
		if ((array.Length != 5 && array.Length != 4) || array[0] != text || !int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result != node || !int.TryParse(array[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out wave) || wave <= 0)
		{
			wave = 0;
			return false;
		}
		inProgress = array[3] == "1";
		if (array.Length == 5)
		{
			int.TryParse(array[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out fastClears);
		}
		return true;
	}

	public static void Forget()
	{
		AppDomain.CurrentDomain.SetData(Slot, null);
	}
}
