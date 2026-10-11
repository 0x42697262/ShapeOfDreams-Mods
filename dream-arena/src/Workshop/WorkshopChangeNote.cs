using System;
using System.IO;
using DreamArena.Diagnostics;
using DreamArena.Game;
using DreamArena.Identity;
using DreamArena.Text;

namespace DreamArena.Workshop;

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
			string path = Path.Combine(item.path, GameNames.ModAboutFolder, ModIdentity.ChangeNoteFile);
			string note = File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
			if (note.Length == 0)
			{
				Log.Warn(LogText.ChangeNoteMissing);
				return;
			}

			_pending = note;
		}
		catch (Exception e)
		{
			Log.Warn(LogText.ChangeNoteFailed(e.Message));
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
		Log.Info(LogText.ChangeNoteAttached(changeNote.Length));
	}

	public static void End()
	{
		_pending = null;
	}

	private static bool IsOurs(ModItem item)
	{
		DreamArenaMod mod = DreamArenaMod.Current;
		return item != null && item.metadata != null && mod != null && mod.mod != null && mod.mod.metadata != null
		       && item.metadata.id == mod.mod.metadata.id;
	}
}
