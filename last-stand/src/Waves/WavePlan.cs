namespace LastStand.Waves;

internal sealed class WavePlan
{
	public int Number { get; set; }
	public int RegularCount { get; set; }
	public bool IsSurge { get; set; }
	public int HunterCount { get; set; }
	public int MiniBossCount { get; set; }
	public int PainCount { get; set; }
	public int HeroicBossCount { get; set; }
	public int AmbientLevel { get; set; }
	public int MaxAlive { get; set; }
	public double HealthBonusPercent { get; set; }
	public double DamageBonusPercent { get; set; }
	public int ForeignTypesUnlocked { get; set; }
	public bool NewForeignTypes { get; set; }
	public int HuntLevel { get; set; }
	public int MirageSkinsUnlocked { get; set; }
	public bool NewMirageSkin { get; set; }
	public double MirageChance { get; set; }

	public bool IsHunted => HuntLevel > 0;
}
