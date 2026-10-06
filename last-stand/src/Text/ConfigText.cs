namespace LastStand.Text;

internal static class ConfigText
{
	public const string ArenaHeader = "Arena";
	public const string WavesHeader = "Waves";
	public const string ScalingHeader = "Monster scaling";
	public const string MiniBossHeader = "Minibosses";
	public const string PainHeader = "Pain minibosses";
	public const string HeroicBossHeader = "Heroic bosses";
	public const string ForeignHeader = "Monsters from other worlds";
	public const string HunterHeader = "Hunters";
	public const string MirageHeader = "Mirage variants";
	public const string ItemDropsHeader = "Item drops";
	public const string EnemyRewardsHeader = "Enemy rewards";
	public const string ChaosRarityHeader = "Chaos rarity";

	public const string StripRoomModifiersLabel = "Plain arena (no room modifiers)";
	public const string StripRoomModifiersDescription =
		"Removes the room modifiers the world rolls onto arena nodes and blocks new ones, so no extra " +
		"shrines, props, rifts or minibosses appear in the arena. Last Stand's own Pain and hunter zone " +
		"effects and the hero soul used for reviving still work.";

	public const string ShowWaveInHudLabel = "Show wave in HUD";
	public const string ShowWaveInHudDescription =
		"Replaces the location readout in the top right with the current wave and how many monsters " +
		"are left. Only you see it: other players' HUDs are drawn by their own unmodded game.";

	public const string HeroicBossFromWaveLabel = "First heroic boss wave";
	public const string HeroicBossFromWaveDescription =
		"Wave of the first heroic boss. The defaults put one right before every 50th wave: 49, 99, 149...";
	public const string HeroicBossIntervalLabel = "Heroic boss every N waves";
	public const string HeroicBossIntervalDescription = "Waves between heroic boss waves.";
	public const string HeroicBossStepLabel = "Waves per extra heroic boss";
	public const string HeroicBossStepDescription =
		"Heroic boss waves this many waves after the first bring one more boss: 1 at 49 and 99, 2 at 149 and " +
		"199, 3 at 249, and so on.";
	public const string MaxHeroicBossesLabel = "Max heroic bosses per wave";
	public const string MaxHeroicBossesDescription = "Upper limit on heroic bosses in one wave.";
	public const string HeroicBossDelayLabel = "Heroic boss entrance delay (s)";
	public const string HeroicBossDelayDescription =
		"Seconds after the last monster and miniboss of the wave has spawned before the heroic boss enters.";
	public const string HeroicBossHealthMultiplierLabel = "Heroic boss health multiplier";
	public const string HeroicBossDamageMultiplierLabel = "Heroic boss damage multiplier";
	public const string HeroicBossStatMultiplierDescription =
		"Multiplies the heroic boss's final value, after the game's boss scaling and the per-wave bonus. " +
		"10 means 10 times as much.";
	public const string HeroicBossAbilityHasteLabel = "Heroic boss ability haste";
	public const string HeroicBossAbilityHasteDescription =
		"Extra ability haste for heroic bosses, so they use their skills more often. Cooldowns scale by " +
		"1 / (1 + haste / 100), down to the game's own floor: 100 is about twice as often, 0 is off.";
	public const string HeroicBossTypesLabel = "Heroic boss types";
	public const string HeroicBossTypesDescription =
		"Bosses to pick from at random. None checked means no heroic bosses. Changes apply on the next arena.";
	public const string HeroicBossChaosLabel = "Heroic boss Corrupted Chaos rewards";
	public const string HeroicBossChaosDescription =
		"Corrupted Chaos shrines (the kind Pain minibosses leave) each heroic boss drops when it dies.";
	public const string ItemRarityFloorLabel = "Rarity floor with item multipliers";
	public const string ItemRarityFloorDescription =
		"When a miniboss's or boss's item multiplier pushes the chances past 100%, the excess is taken from the " +
		"lowest rarities first, but never below this, then from the next rarity up. Only cuts, never raises.";
	public const string HeroicBossDropMultiplierLabel = "Heroic boss item chance multiplier";
	public const string HeroicBossDreamDustLabel = "Heroic boss dream dust";
	public const string HeroicBossGoldLabel = "Heroic boss bonus gold";

	public const string RandomBossMusicLabel = "Random boss music";
	public const string RandomBossMusicDescription =
		"Each time the arena loads, plays a random zone boss fight track. Music is played by each " +
		"player's own game, so only the host hears it; other players hear the usual music.";

	public const string ArenaRoomLabel = "Arena room";
	public const string ArenaRoomDescription =
		"Boss room every combat node loads as. Takes effect on the next run.";

	public const string PrimusWorld = "Primus";
	public const string ForestWorld = "Forest";
	public const string DarkCaveWorld = "Dark Cave";
	public const string DespairWorld = "Despair";
	public const string SkyWorld = "Sky";
	public const string SnowMountainWorld = "Snow Mountain";
	public const string LavaLandWorld = "Lava Land";
	public const string InkWorld = "Ink";

	public const string WindowTitle = "Last Stand";
	public const string WindowNote = "Host only. Other players do not need this mod.";
	public const string ApplyButton = "Apply";
	public const string CancelButton = "Cancel";
	public const string LobbyButton = "Last Stand";

	public static string ChoiceLabel(string name, string world)
	{
		return name == null ? world : $"{name} ({world})";
	}

	public static string UnknownChoiceLabel(string value)
	{
		return $"{value} (not in the list)";
	}

	public const string FirstWaveDelayLabel = "First wave delay (s)";
	public const string FirstWaveDelayDescription = "Seconds between arriving in the arena and the first wave.";

	public const string IntermissionLabel = "Break between waves (s)";
	public const string IntermissionDescription =
		"A wave ends when every monster in it is dead. The next one starts after this many seconds.";

	public const string RegularBaseLabel = "Monsters in wave 1";
	public const string RegularBaseDescription = "Regular monsters in the first wave, for one player.";

	public const string RegularGrowthLabel = "Extra monsters per wave";
	public const string RegularGrowthDescription = "Regular monsters added with every wave after the first.";

	public const string SurgeIntervalLabel = "Surge every N waves";
	public const string SurgeIntervalDescription =
		"Every Nth wave is a surge: regular monsters, hunters and the alive cap are multiplied for that wave. " +
		"Minibosses, Pain minibosses and heroic bosses are not affected.";
	public const string SurgeMultiplierLabel = "Surge monster multiplier";
	public const string SurgeMultiplierDescription = "Regular monsters, hunters and the alive cap on a surge wave are multiplied by this. 1 turns surges off.";

	public const string ExtraPlayerShareLabel = "Extra player share";
	public const string ExtraPlayerShareDescription =
		"Each player past the first adds this fraction of the wave again. 0.5 means two players face 1.5x.";

	public const string MaxAliveBaseLabel = "Max monsters alive";
	public const string MaxAliveBaseDescription =
		"Cap on monsters alive at once. Spawns over the cap wait for an opening, which keeps the " +
		"host's frame rate steady.";

	public const string MaxAlivePerPlayerLabel = "Max alive per extra player";
	public const string MaxAlivePerPlayerDescription = "Added to the cap for each player past the first. 0 keeps one cap for everyone.";

	public const string SpawnIntervalLabel = "Spawn interval (s)";
	public const string SpawnIntervalDescription = "Seconds between spawn batches while a wave arrives.";

	public const string SpawnsPerIntervalLabel = "Spawns per batch";
	public const string SpawnsPerIntervalDescription = "Monsters released per spawn batch.";

	public const string MinSpawnDistanceLabel = "Min spawn distance from heroes (m)";
	public const string MinSpawnDistanceDescription =
		"Monsters spawn at random spots across the whole arena room, but not closer than this to any hero.";

	public const string HealthPerWaveLabel = "Health per wave (%)";
	public const string HealthPerWaveDescription =
		"Every monster gets this much extra max health for each wave after the first. 8 means +80% by wave 11.";

	public const string DamagePerWaveLabel = "Damage per wave (%)";
	public const string DamagePerWaveDescription =
		"Every monster gets this much extra attack and ability damage for each wave after the first.";

	public const string StartingAmbientLevelLabel = "Starting monster level";
	public const string StartingAmbientLevelDescription =
		"The game's ambient level, which sets the level monsters spawn at, in wave 1.";

	public const string WavesPerAmbientLevelLabel = "Waves per monster level";
	public const string WavesPerAmbientLevelDescription = "Every this many waves, the ambient level rises by one.";

	public const string MiniBossFromWaveLabel = "First miniboss wave";
	public const string MiniBossFromWaveDescription = "First wave that brings minibosses.";

	public const string MiniBossIntervalLabel = "Waves between miniboss waves";
	public const string MiniBossIntervalDescription = "Minibosses come every this many waves.";

	public const string MiniBossOccurrencesPerExtraLabel = "Miniboss waves per extra miniboss";
	public const string PainOccurrencesPerExtraLabel = "Pain waves per extra Pain miniboss";
	public const string OccurrencesPerExtraDescription =
		"After this many of these waves, each one brings one more. 2 means 1, 1, 2, 2, 3...";

	public const string MaxMiniBossesLabel = "Max minibosses per wave";
	public const string MiniBossHealthMultiplierLabel = "Miniboss health multiplier";
	public const string MiniBossDamageMultiplierLabel = "Miniboss damage multiplier";
	public const string MiniBossStatMultiplierDescription =
		"Multiplies the miniboss's final value, after the game's miniboss scaling and the per-wave bonus. " +
		"Pain minibosses are not affected. 1 is off.";
	public const string MaxMiniBossesDescription = "Upper limit on regular minibosses in one wave.";

	public const string PainFromWaveLabel = "First Pain wave";
	public const string PainFromWaveDescription = "First wave that brings Pain minibosses.";

	public const string PainIntervalLabel = "Waves between Pain waves";
	public const string PainIntervalDescription = "Pain minibosses come every this many waves.";

	public const string MaxPainLabel = "Max Pain minibosses per wave";
	public const string MaxPainDescription = "Upper limit on Pain minibosses in one wave.";

	public const string PainDamageLabel = "Pain damage (% per s)";
	public const string PainDamageDescription =
		"Percent of max health plus shield every hero loses per second, per living Pain miniboss. " +
		"Never takes a hero below 1 HP.";

	public const string PainHealthBonusLabel = "Pain health bonus (%)";
	public const string PainHealthBonusDescription = "Extra max health on Pain minibosses.";

	public const string PainDamageBonusLabel = "Pain damage bonus (%)";
	public const string PainDamageBonusDescription = "Extra attack and ability damage on Pain minibosses.";

	public const string ForeignFromWaveLabel = "First new monster wave";
	public const string ForeignFromWaveDescription =
		"Wave that first adds a monster type from another world to the arena's pool.";

	public const string ForeignIntervalLabel = "Waves between new monsters";
	public const string ForeignIntervalDescription =
		"Every this many waves, more monster types from other worlds join, weakest worlds first.";

	public const string ForeignPerUnlockLabel = "New monster types per step";
	public const string ForeignPerUnlockDescription =
		"Monster types added each step. Hunters start once every type has been added.";

	public const string HunterIntervalLabel = "Waves per extra hunter";
	public const string HunterIntervalDescription =
		"Hunters start this many waves after the last new monster type, one per wave, and one more " +
		"joins every this many waves.";

	public const string MaxHuntersLabel = "Max hunters per wave";
	public const string MaxHuntersDescription = "Upper limit on hunters in one wave.";

	public const string HuntedZoneIntervalLabel = "Waves between hunter zones";
	public const string HuntedZoneIntervalDescription =
		"Once hunters have started, every this many waves the arena becomes a hunter zone for the " +
		"wave: the game's falling artillery strikes around the heroes until the wave is cleared.";

	public const string MaxHuntLevelLabel = "Max hunt level";
	public const string MaxHuntLevelDescription =
		"Each hunter zone raises the hunt level by one, up to this. Higher levels mean stronger, " +
		"more frequent artillery.";

	public const string MirageFromWaveLabel = "First Mirage wave";
	public const string MirageFromWaveDescription = "Wave from which monsters can spawn with a Mirage skin.";

	public const string MirageIntervalLabel = "Waves per new Mirage skin";
	public const string MirageIntervalDescription =
		"Every this many waves, the next stronger Mirage skin is added, weakest first.";

	public const string MirageChanceLabel = "Mirage chance (%)";
	public const string MirageChanceDescription =
		"Chance a regular monster or hunter spawns with one of the unlocked Mirage skins.";

	public const string MobItemsPerPlayerLabel = "Monster items per player per wave";
	public const string MobItemsPerPlayerDescription =
		"Most memories and essences regular monsters and hunters can drop in one wave, per player. " +
		"With 1, a solo player gets at most one, and two players get at most one memory and one " +
		"essence. Minibosses are not limited.";

	public const string GuaranteedDropLabel = "Guaranteed drop per wave";
	public const string GuaranteedDropDescription =
		"Anyone who got no memory or essence during a wave gets one when the wave is cleared.";

	public const string RewardGrowthLabel = "Gold growth per wave (%)";
	public const string RewardGrowthDescription = "Bonus gold grows by this percent with every wave. Dream dust stays flat.";

	public const string CommonDropLabel = "Common item chance (%)";
	public const string RareDropLabel = "Rare item chance (%)";
	public const string EpicDropLabel = "Epic item chance (%)";
	public const string LegendaryDropLabel = "Legendary item chance (%)";
	public const string RarityDropDescription =
		"Chance a regular kill drops a memory or essence of this rarity. A kill drops at most one " +
		"item, so with the defaults 60% of regular kills drop nothing.";

	public const string MemoryShareLabel = "Memory share (%)";
	public const string MemoryShareDescription =
		"For a solo player or a miniboss, the chance an item is a memory rather than an essence. " +
		"With more players, monster drops alternate to stay even.";

	public const string RegularDropMultiplierLabel = "Regular: item chance multiplier";
	public const string MiniBossDropMultiplierLabel = "Miniboss: item chance multiplier";
	public const string PainDropMultiplierLabel = "Pain: item chance multiplier";
	public const string DropMultiplierDescription =
		"Multiplies every rarity's item chance for kills of this tier. Rarities are rolled rarest " +
		"first, so if the total passes 100% the common chance is what gets cut.";

	public const string ChaosLegendaryLabel = "Chaos reward: legendary chance (%)";
	public const string ChaosEpicLabel = "Chaos reward: epic chance (%)";
	public const string ChaosRareLabel = "Chaos reward: rare chance (%)";
	public const string ChaosRarityDescription =
		"Rarity of the chaos reward a miniboss drops on death. The rest of the time it is common. " +
		"Replaces the game's own roll, which gave a legendary almost every time.";

	public const string ItemTargetWaveLabel = "Item chances reach target at wave";
	public const string ItemTargetWaveDescription =
		"Memory and essence chances move in a straight line from their wave 1 values to their target " +
		"values over the waves up to this one, then stay there.";
	public const string CommonAtTargetLabel = "Common chance at target wave (%)";
	public const string RareAtTargetLabel = "Rare chance at target wave (%)";
	public const string EpicAtTargetLabel = "Epic chance at target wave (%)";
	public const string LegendaryAtTargetLabel = "Legendary chance at target wave (%)";
	public const string ItemAtTargetDescription =
		"Chance a regular kill drops a memory or essence of this rarity once the target wave is reached. " +
		"With all four adding up to 100%, every regular kill rolls an item there, still capped per player.";

	public const string ChaosTargetWaveLabel = "Chaos chances reach target at wave";
	public const string ChaosTargetWaveDescription =
		"Chaos reward chances move in a straight line from their wave 1 values to their target values " +
		"over the waves up to this one, then stay there.";
	public const string ChaosLegendaryAtTargetLabel = "Chaos reward: legendary at target wave (%)";
	public const string ChaosEpicAtTargetLabel = "Chaos reward: epic at target wave (%)";
	public const string ChaosRareAtTargetLabel = "Chaos reward: rare at target wave (%)";
	public const string ChaosAtTargetDescription =
		"Chaos reward chance of this rarity once the target wave is reached. Common is whatever is left.";

	public const string RegularDreamDustLabel = "Regular: dream dust";
	public const string RegularGoldLabel = "Regular: bonus gold";
	public const string MiniBossDreamDustLabel = "Miniboss: dream dust";
	public const string MiniBossGoldLabel = "Miniboss: bonus gold";
	public const string PainDreamDustLabel = "Pain: dream dust";
	public const string PainGoldLabel = "Pain: bonus gold";

	public const string DreamDustDescription = "Dream dust a kill of this tier drops, the same every wave.";
	public const string GoldDescription = "Gold a kill of this tier drops in wave 1, on top of normal kill gold.";
}
