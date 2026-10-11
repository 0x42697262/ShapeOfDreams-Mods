namespace SodLib.Game;

internal static class ChatNotices
{
	public static void Broadcast(string content)
	{
		ChatManager chat = NetworkedManagerBase<ChatManager>.softInstance;
		if (chat == null)
		{
			return;
		}

		chat.BroadcastMessage(new ChatManager.Message
		{
			type = ChatManager.MessageType.Raw,
			content = content,
		});
	}

	public static void Tell(DewPlayer player, string content)
	{
		ChatManager chat = NetworkedManagerBase<ChatManager>.softInstance;
		if (chat == null || player == null || player.connectionToClient == null)
		{
			return;
		}

		chat.SendChatMessage(player.connectionToClient, new ChatManager.Message
		{
			type = ChatManager.MessageType.Raw,
			content = content,
		});
	}
}
