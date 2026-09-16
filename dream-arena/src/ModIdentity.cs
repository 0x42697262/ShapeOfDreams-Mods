namespace DreamArena;

/// <summary>
/// Names this mod stamps onto objects it creates, so teardown can find them
/// and so nothing collides with another mod.
/// </summary>
internal static class ModIdentity
{
	public const string Tag = "DreamArena";

	/// <summary>Suffix on every scene object this mod creates.</summary>
	public const string OwnedSuffix = " (" + Tag + ")";

	public const string LogPrefix = "[" + Tag + "] ";

	public const string ConfigWindowName = "Dream Arena Config" + OwnedSuffix;
	public const string LobbyButtonName = "Dream Arena Button" + OwnedSuffix;

	public static bool IsOurs(string objectName)
	{
		return objectName != null && objectName.EndsWith(OwnedSuffix, System.StringComparison.Ordinal);
	}
}
