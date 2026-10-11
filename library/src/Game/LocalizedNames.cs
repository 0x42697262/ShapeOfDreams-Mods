namespace SodLib.Game;

internal static class LocalizedNames
{
	public static string Find(string typeName)
	{
		return DewLocalization.TryGetUIValue(typeName + SharedGameNames.LocalizedNameSuffix, out string localized)
			? localized
			: null;
	}
}
