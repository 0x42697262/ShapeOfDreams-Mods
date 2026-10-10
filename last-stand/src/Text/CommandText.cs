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
		"Cheat: spawn a weak level 1 heroic boss near you that drops its Corrupted Chaos rewards (host, in the " +
		"arena, needs cheat commands on). Optional: boss type, e.g. Mon_Forest_BossDemon";

	public const string Gold = "laststand_gold";
	public const string GoldDescription =
		"Cheat: grant the given gold to every player, e.g. laststand_gold 5000 (host, needs cheat commands on)";

	public const string Skip = "laststand_skip";
	public const string SkipDescription =
		"Cheat: end the current wave now (monsters removed, no drops) and start the next at its own " +
		"difficulty. Optional: how many waves, e.g. laststand_skip 10 (host, in the arena, needs cheat commands on)";

	public const string DreamDust = "laststand_dust";
	public const string DreamDustDescription =
		"Cheat: grant the given dream dust to every player, e.g. laststand_dust 5000 (host, needs cheat commands on)";

	public const string Revive = "laststand_revive";
	public const string ReviveDescription =
		"Cheat: revive every knocked-out hero at full health now (host, needs cheat commands on)";

	public const string Level = "laststand_level";
	public const string LevelDescription =
		"Cheat: set every hero to the given level, capped at the game's max, e.g. laststand_level 20 (host, needs " +
		"cheat commands on)";

	public const string Essence = "laststand_essence";
	public const string EssenceDescription =
		"Cheat: drop an essence near you. Rarity (common, rare, epic, legendary) or part of a type or display " +
		"name; optional quality, e.g. laststand_essence legendary 200 (host, needs cheat commands on). The " +
		"game's listgems lists names";

	public const string Memory = "laststand_memory";
	public const string MemoryDescription =
		"Cheat: drop a memory near you. Rarity or part of a type or display name; optional level, e.g. " +
		"laststand_memory epic 10 (host, needs cheat commands on). The game's listskills lists names";

	public const string SecondWind = "laststand_secondwind";
	public const string SecondWindDescription =
		"Cheat: give every hero the Second Wind star's effect with the given charges (each one survives a fatal " +
		"hit at 25% health, then is invulnerable for a while); optional invulnerable seconds, default 3.5. 0 " +
		"removes it, e.g. laststand_secondwind 3 5 (host, needs cheat commands on)";

	public const string EssenceKind = "essence";
	public const string MemoryKind = "memory";
	public const string QualityStat = "quality";
	public const string LevelStat = "level";

	public const string Config = "laststand_config";
	public const string ConfigDescription = "Open the Last Stand settings window";

	public const string Solo = "laststand_solo";
	public const string SoloDescription = "Start a solo Last Stand run from the title screen";
}
