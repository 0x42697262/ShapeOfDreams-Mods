using System;
using System.IO;

namespace SodLib.Workshop;

internal static class WorkshopChangeNote
{
	private static string _pending;

	public static void Begin(ModItem item)
	{
		_pending = null;
		if (!IsOurs(item))
		{
			return;
		}

		try
		{
			string path = Path.Combine(item.path, SharedGameNames.ModAboutFolder, ModIdentity.ChangeNoteFile);
			string note = File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
			if (note.Length == 0)
			{
				Log.Warn(SharedLogText.ChangeNoteMissing);
				return;
			}

			_pending = note;
		}
		catch (Exception e)
		{
			Log.Warn(SharedLogText.ChangeNoteFailed(e.Message));
		}
	}

	public static void Apply(ref string changeNote)
	{
		if (_pending == null || !string.IsNullOrEmpty(changeNote))
		{
			return;
		}

		changeNote = _pending;
		_pending = null;
		Log.Info(SharedLogText.ChangeNoteAttached(changeNote.Length));
	}

	public static void End()
	{
		_pending = null;
	}

	private static bool IsOurs(ModItem item)
	{
		ModBehaviour mod = ModHooks.Behaviour();
		return item != null && item.metadata != null && mod != null && mod.mod != null && mod.mod.metadata != null
		       && item.metadata.id == mod.mod.metadata.id;
	}
}
