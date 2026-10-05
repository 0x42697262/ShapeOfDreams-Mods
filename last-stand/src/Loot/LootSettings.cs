using System.Collections.Generic;
using LastStand.Combat;

namespace LastStand.Loot;

internal sealed class LootSettings
{
	private readonly IReadOnlyDictionary<EnemyTier, TierLoot> _tiers;

	public RarityTable Items { get; }

	public double MemoryShare { get; }

	public RarityTable Chaos { get; }

	public int MobItemsPerPlayerPerWave { get; }

	public double GrowthPerWave { get; }

	public bool GuaranteedDropPerWave { get; }

	public int HeroicBossChaosRewards { get; }

	public LootSettings(RarityTable items, double memoryShare, RarityTable chaos,
		IReadOnlyDictionary<EnemyTier, TierLoot> tiers, int mobItemsPerPlayerPerWave, double growthPerWave,
		bool guaranteedDropPerWave, int heroicBossChaosRewards)
	{
		Items = items;
		MemoryShare = memoryShare;
		Chaos = chaos;
		_tiers = tiers;
		MobItemsPerPlayerPerWave = mobItemsPerPlayerPerWave;
		GrowthPerWave = growthPerWave;
		GuaranteedDropPerWave = guaranteedDropPerWave;
		HeroicBossChaosRewards = heroicBossChaosRewards;
	}

	public TierLoot For(EnemyTier tier)
	{
		return _tiers[tier];
	}
}
