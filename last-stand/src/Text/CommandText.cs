namespace LastStand.Text;

internal static class CommandText
{
	public const string Status = "laststand_status";
	public const string StatusDescription = "Report the Last Stand session state";

	public const string NextWave = "laststand_next";
	public const string NextWaveDescription = "Start the next Last Stand wave now, even if monsters remain (host only)";

	public const string Wave = "laststand_wave";
	public const string WaveDescription =
		"Start the given Last Stand wave now, e.g. laststand_wave 10 (host, in the arena). Monsters already out stay";

	public const string TestBoss = "laststand_testboss";
	public const string TestBossDescription =
		"Spawn a weak level 1 heroic boss near you that drops its Corrupted Chaos rewards (host, in the arena). " +
		"Optional: boss type, e.g. Mon_Forest_BossDemon";

	public const string Config = "laststand_config";
	public const string ConfigDescription = "Open the Last Stand settings window";

	public const string Solo = "laststand_solo";
	public const string SoloDescription = "Start a solo Last Stand run from the title screen";
}
