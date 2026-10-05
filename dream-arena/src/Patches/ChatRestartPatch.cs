using HarmonyLib;
using Mirror;

namespace DreamArena.Patches;

/// <summary>
/// Lets any player type <c>-restart</c> in chat to call a vote on sending
/// everyone back to camp with a clean slate.
/// </summary>
/// <remarks>
/// Every chat line, the host's included, reaches the server through
/// <c>ChatManager.CmdSendChatMessage</c>, so a postfix on its body sees what
/// each player typed. Vanilla clients need nothing: they just send chat. The
/// line itself is still posted as normal, so everyone sees who asked, and the
/// vote follows it (see <see cref="RestartVote"/>).
/// </remarks>
[HarmonyPatch(typeof(ChatManager), "UserCode_CmdSendChatMessage__String__NetworkConnectionToClient")]
internal static class ChatRestartPatch
{
	private const string Command = "-restart";

	[HarmonyPostfix]
	private static void Postfix(string content, NetworkConnectionToClient sender)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (!NetworkServer.active || mod == null
		    || !mod.config.arenaEnabled || !mod.config.chatRestart
		    || content == null || sender == null)
		{
			return;
		}

		if (!string.Equals(content.Trim(), Command, System.StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		DewPlayer player = sender.GetPlayer();
		if (!RestartVote.TryBegin(player, out string reason))
		{
			MatchRestart.Tell(player, $"Can't restart: {reason}");
		}
	}
}
