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

	private const int RunField = 0;

	private const int NodeField = 1;

	private const int WaveField = 2;

	private const int FlagField = 3;

	private const int FormatField = 4;

	private const int PaceField = 5;

	public static string CurrentRunKey()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		return game != null && !string.IsNullOrEmpty(game.runId) ? game.runId : null;
	}

	public static void Save(int node, int wave, bool inProgress, PaceLedger pace)
	{
		string run = CurrentRunKey();
		if (run != null)
		{
			Store(run, node, wave, inProgress, pace);
		}
	}

	public static bool TryLoad(int node, out int wave, out bool inProgress, out PaceLedger pace)
	{
		string run = CurrentRunKey();
		if (run == null || !TryParse(Stored(), out string savedRun, out int savedNode, out wave, out inProgress, out pace)
		    || (savedRun != run && savedRun != ModIdentity.ContinuedRunKey) || savedNode != node)
		{
			wave = 0;
			inProgress = false;
			pace = new PaceLedger();
			return false;
		}

		if (savedRun != run)
		{
			Store(run, node, wave, inProgress, pace);
		}

		return true;
	}

	public static void ForgetContinued()
	{
		if (TryParse(Stored(), out string savedRun, out _, out _, out _, out _)
		    && savedRun == ModIdentity.ContinuedRunKey)
		{
			Forget();
		}
	}

	public static string Snapshot()
	{
		string run = CurrentRunKey();
		return run != null && TryParse(Stored(), out string savedRun, out _, out _, out _, out _) && savedRun == run
			? Stored()
			: null;
	}

	public static bool TryRestore(string saved, out int wave, out bool wasInProgress)
	{
		wave = 0;
		wasInProgress = false;
		if (!TryParse(saved, out _, out int node, out int savedWave, out wasInProgress, out PaceLedger pace))
		{
			Forget();
			return false;
		}

		wave = wasInProgress ? savedWave - 1 : savedWave;
		if (wave < Tuning.FirstWave)
		{
			Forget();
			return false;
		}

		Store(ModIdentity.ContinuedRunKey, node, wave, false, pace);
		return true;
	}

	public static void Forget()
	{
		AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, null);
	}

	private static string Stored()
	{
		return AppDomain.CurrentDomain.GetData(ModIdentity.ProgressSlot) as string;
	}

	private static void Store(string run, int node, int wave, bool inProgress, PaceLedger pace)
	{
		string data = string.Join(Separator.ToString(), run, node.ToString(CultureInfo.InvariantCulture),
			wave.ToString(CultureInfo.InvariantCulture),
			inProgress ? Tuning.ProgressInProgressFlag : Tuning.ProgressBetweenWavesFlag,
			ModIdentity.ProgressPaceFormat, pace.Serialize());
		AppDomain.CurrentDomain.SetData(ModIdentity.ProgressSlot, data);
	}

	private static bool TryParse(string data, out string run, out int node, out int wave, out bool inProgress,
		out PaceLedger pace)
	{
		run = null;
		node = 0;
		wave = 0;
		inProgress = false;
		pace = new PaceLedger();
		if (data == null)
		{
			return false;
		}

		string[] parts = data.Split(Separator);
		if (parts.Length < OldestFieldCount || parts.Length > FieldCount
		    || !int.TryParse(parts[NodeField], NumberStyles.Integer, CultureInfo.InvariantCulture, out node)
		    || !int.TryParse(parts[WaveField], NumberStyles.Integer, CultureInfo.InvariantCulture, out wave)
		    || wave <= 0)
		{
			return false;
		}

		run = parts[RunField];
		inProgress = parts[FlagField] == Tuning.ProgressInProgressFlag;
		if (parts.Length == FieldCount && parts[FormatField] == ModIdentity.ProgressPaceFormat)
		{
			pace = PaceLedger.Parse(parts[PaceField]);
		}

		return true;
	}
}
