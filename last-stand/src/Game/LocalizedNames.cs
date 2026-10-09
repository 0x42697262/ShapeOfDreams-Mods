namespace LastStand.Game;

internal static class LocalizedNames
{
	public static string Find(string typeName)
	{
		return DewLocalization.TryGetUIValue(typeName + GameNames.LocalizedNameSuffix, out string localized)
			? localized
			: null;
	}
}
