namespace LastStand.Game;

internal static class ModifierPanel
{
	public static void Hide()
	{
		UI_InGame_RoomModDisplay panel = SingletonBehaviour<UI_InGame_RoomModDisplay>.softInstance;
		if (panel != null && panel.gameObject.activeSelf)
		{
			panel.gameObject.SetActive(false);
		}
	}
}
