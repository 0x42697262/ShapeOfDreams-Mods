namespace LastStand.Game;

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
}
