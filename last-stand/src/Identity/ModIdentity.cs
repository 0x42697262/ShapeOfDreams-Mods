namespace SodLib.Identity;

internal static partial class ModIdentity
{
	public const string Tag = "LastStand";

	public const string DisplayName = "Last Stand";

	public const string SaveKey = "laststand";

	public const string LobbyModeValue = "LastStand_Mode";

	public static readonly bool PreselectInLobby = true;

	public const string StartingGoldSlot = Tag + ".StartingGold";

	public const string ProgressSlot = Tag + ".Progress";

	public const string ProgressPaceFormat = "pace";

	public const string NoPlayerKey = "x";

	public const string SteamPlayerKeyPrefix = "s";

	public const string ProfilePlayerKeyPrefix = "g";

	public const string NetPlayerKeyPrefix = "n";

	public const string SoloButtonName = DisplayName + " Button" + OwnedSuffix;
}
