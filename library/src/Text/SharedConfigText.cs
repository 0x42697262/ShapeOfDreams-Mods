namespace SodLib.Text;

internal static class SharedConfigText
{
	public const string ApplyButton = "Apply";

	public const string CancelButton = "Cancel";

	public const string PrimusWorld = "Primus";

	public const string ForestWorld = "Forest";

	public const string DarkCaveWorld = "Dark Cave";

	public const string DespairWorld = "Despair";

	public const string SkyWorld = "Sky";

	public const string SnowMountainWorld = "Snow Mountain";

	public const string LavaLandWorld = "Lava Land";

	public const string InkWorld = "Ink";

	public static string ChoiceLabel(string name, string detail)
	{
		return name == null ? detail : $"{name} ({detail})";
	}

	public static string UnknownChoiceLabel(string value)
	{
		return $"{value} (not in the list)";
	}
}
