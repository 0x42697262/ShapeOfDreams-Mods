namespace SodLib.Game;

internal sealed class VersionAnnouncer
{
	private GameManager _announced;

	public void Tick()
	{
		GameManager game = NetworkedManagerBase<GameManager>.softInstance;
		string version = ModEntry.Active?.Version;
		if (game == null || game == _announced || !game.IsLazyCallReady() || string.IsNullOrEmpty(version))
		{
			return;
		}

		_announced = game;
		ChatNotices.Broadcast(SharedMessages.Version(ModIdentity.DisplayName, version));
		Log.Info(SharedLogText.VersionAnnounced(version));
	}
}
