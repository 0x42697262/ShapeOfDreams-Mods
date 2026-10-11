using System;
using System.IO;
using Steamworks;

namespace SodLib.Workshop;

internal static class WorkshopChangeNote
{
	private static bool _uploading;
	private static string _pending;

	public static void Begin(PublishedFileId_t id)
	{
		_uploading = false;
		_pending = null;
		ModItem item = OurItem(id);
		if (item == null)
		{
			return;
		}

		_uploading = true;
		Log.Info(SharedLogText.ChangeNoteUploadStarted(id.m_PublishedFileId, item.path));
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
		if (!_uploading)
		{
			return;
		}

		_uploading = false;
		if (_pending == null)
		{
			return;
		}

		changeNote = _pending;
		_pending = null;
		Log.Info(SharedLogText.ChangeNoteAttached(changeNote.Length));
	}

	private static ModItem OurItem(PublishedFileId_t id)
	{
		ModItem item = ModHooks.Behaviour()?.mod;
		return item != null && id.m_PublishedFileId != 0 && item.publishedFileId == id.m_PublishedFileId ? item : null;
	}
}
