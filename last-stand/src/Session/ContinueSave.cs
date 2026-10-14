using System;
using System.Collections.Generic;
using LastStand.Text;

namespace LastStand.Session;

internal static class ContinueSave
{
	public static void Write(DewPersistence.GameData data)
	{
		try
		{
			string progress = ProgressStore.Snapshot();
			if (data == null || progress == null)
			{
				return;
			}

			data.managerPersistentData[ModIdentity.ContinueSaveKey] = new Dictionary<string, string>
			{
				[ModIdentity.ContinueProgressField] = progress,
			};
			Log.Info(LogText.ContinueSaveWritten(progress));
		}
		catch (Exception e)
		{
			Log.Error(LogText.ContinueSaveFailed, e);
		}
	}

	public static void OnGameStarted()
	{
		if (DewNetworkManager.startSettings?.continueData == null)
		{
			ProgressStore.ForgetContinued();
		}
	}

	public static void Read(DewPersistence.GameData data)
	{
		try
		{
			if (data?.managerPersistentData == null
			    || !data.managerPersistentData.TryGetValue(ModIdentity.ContinueSaveKey,
				    out Dictionary<string, string> fields)
			    || fields == null
			    || !fields.TryGetValue(ModIdentity.ContinueProgressField, out string progress))
			{
				ProgressStore.Forget();
				Log.Info(LogText.ContinueSaveMissing);
				return;
			}

			Log.Info(ProgressStore.TryRestore(progress, out int wave, out bool wasInProgress)
				? LogText.ContinueSaveRestored(wave, wasInProgress)
				: LogText.ContinueSaveUnusable(progress));
		}
		catch (Exception e)
		{
			Log.Error(LogText.ContinueSaveFailed, e);
		}
	}
}
