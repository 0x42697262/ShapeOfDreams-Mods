using HarmonyLib;
using LastStand.Game;
using Mirror;

namespace LastStand.Patches;

[HarmonyPatch(typeof(ChatManager), GameNames.ChatMessageCommand)]
internal static class ChatCommandPatch
{
	[HarmonyPostfix]
	private static void Postfix(string content, NetworkConnectionToClient sender)
	{
		if (NetworkServer.active && ModMode.IsHosting() && sender != null)
		{
			LastStandMod.Current?.Session?.OnChat(sender.connectionId, content);
		}
	}
}
