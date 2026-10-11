

namespace LastStand.Waves;

internal sealed class WavePlan
{
	public int Number { get; set; }

	public int RegularCount { get; set; }

	public bool IsSurge { get; set; }

	public int HunterCount { get; set; }

	public int UnmergedMobCount { get; set; }

	public double MergeMultiplier { get; set; } = MergeRule.NoChange;

	public double MiniBossHealthMultiple { get; set; }

	public double HeroicBossHealthMultiple { get; set; }

	public int MiniBossCount { get; set; }

	public int PainCount { get; set; }

	public int HeroicBossCount { get; set; }

	public double HeroicBossWindowSeconds { get; set; }

	public double HeroicBossHaste { get; set; }

	public int AmbientLevel { get; set; }

	public int MaxAlive { get; set; }

	public double HealthBonusPercent { get; set; }

	public double DamageBonusPercent { get; set; }

	public double EmpoweredMultiplier { get; set; } = 1;

	public double FastClearMultiplier { get; set; } = 1;

	public double HeroicFastClearMultiplier { get; set; } = 1;

	public double HeroicKillMultiplier { get; set; } = 1;

	public int ForeignTypesUnlocked { get; set; }

	public bool NewForeignTypes { get; set; }

	public int HuntLevel { get; set; }

	public int MirageSkinsUnlocked { get; set; }

	public bool NewMirageSkin { get; set; }

	public double MirageChance { get; set; }

	public bool HasEnvironment { get; set; }

	public double FriendlyFireChance { get; set; }

	public bool IsHunted => HuntLevel > 0;

	public bool HasBossHealthFloor => MiniBossHealthMultiple > 0 || HeroicBossHealthMultiple > 0;

	public bool IsEmpowered => EmpoweredMultiplier != 1;

	public bool IsMerged => MergeMultiplier != MergeRule.NoChange;

	public int TotalMonsters => RegularCount + HunterCount + MiniBossCount + PainCount + HeroicBossCount;

	public int FullStrengthMonsters => UnmergedMobCount + MiniBossCount + PainCount + HeroicBossCount;

	public bool HasBoss => MiniBossCount > 0 || PainCount > 0 || HeroicBossCount > 0;
}
