namespace SodLib.Identity;

internal static partial class ModIdentity
{
	public const string Tag = "DreamArena";

	public const string DisplayName = "Dream Arena";

	public const string SaveKey = "dreamarena_pvp";

	public const string LobbyModeValue = "DreamArena_PvP";

	public static readonly bool PreselectInLobby = false;

	public const string DummyName = "Training Dummy" + OwnedSuffix;

	public const string DummyLifeEffectId = Tag + "_DummyLife";

	public static string RemnantName(string description)
	{
		return description + OwnedSuffix;
	}
}
