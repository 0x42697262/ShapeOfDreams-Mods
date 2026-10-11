using LastStand.Config;

namespace LastStand.Text;

internal static class ConfigText
{
	public const string RunHeader = "Run";

	public const string ArenaHeader = "Arena";

	public const string DeveloperHeader = "Developer";

	public const string ShrinesHeader = "Edge shrines";

	public const string HatredHeader = "Hatred Remnant and curses";

	public const string TimingHeader = "Timing";

	public const string WaveSizeHeader = "Wave size";

	public const string SpawningHeader = "Spawning";

	public const string ScalingHeader = "Monster scaling";

	public const string MiniBossHeader = "Minibosses";

	public const string PainHeader = "Pain minibosses";

	public const string HeroicBossHeader = "Heroic bosses";

	public const string ForeignHeader = "Monsters from other worlds";

	public const string HunterHeader = "Hunters";

	public const string MirageHeader = "Mirage variants";

	public const string EnvironmentHeader = "Environments";

	public const string EmpoweredHeader = "Empowered waves";

	public const string EmpoweredFromWaveLabel = "First empowered wave";

	public const string EmpoweredFromWaveDescription = "First wave whose monsters all get the multiplier below.";

	public const string EmpoweredIntervalLabel = "Empowered every N waves";

	public const string EmpoweredIntervalDescription = "Waves between empowered waves.";

	public const string EmpoweredMultiplierLabel = "Empowered health and damage (x)";

	public const string EmpoweredMultiplierDescription =
		"Health and damage multiplier for every monster of an empowered wave, bosses included. 1 = off.";

	public const string FastClearHeader = "Fast clear";

	public const string FriendlyFireHeader = "Dark urge";

	public const string ItemDropsHeader = "Item drops";

	public const string EnemyRewardsHeader = "Enemy rewards";

	public const string ChaosRarityHeader = "Chaos rarity";

	public const string BossMusicLabel = "Boss music";

	public const string BossMusicDescription =
		"The arena plays its boss room's own boss music instead of the zone's music, for every player.";

	public const string StripRoomModifiersLabel = "Plain arena (no room modifiers)";

	public const string StripRoomModifiersDescription =
		"Strips world-gen room modifiers from the arena and blocks new ones. No stray shrines, props, " +
		"rifts or minibosses. Pain, hunter zones and environments still work.";

	public const string ShowWaveInQuestTrackerLabel = "Show wave in quest tracker";

	public const string ShowWaveInQuestTrackerDescription =
		"Wave and monsters left in the quest tracker on the right. Every player sees it, vanilla clients included.";

	public const string HeroicBossFromWaveLabel = "First heroic boss wave";

	public const string HeroicBossFromWaveDescription =
		"First heroic boss wave. Defaults: 50, 100, then every 25 (125, 150, 175…).";

	public const string HeroicBossIntervalLabel = "Heroic boss every N waves";

	public const string HeroicBossIntervalDescription =
		"Waves between heroic boss waves, before the switch wave.";

	public const string HeroicBossFasterFromWaveLabel = "Heroic bosses more often from wave";

	public const string HeroicBossFasterFromWaveDescription =
		"From this wave, heroic bosses use the later interval, counted from the last boss wave (100 + 25 = " +
		"125).";

	public const string HeroicBossLateIntervalLabel = "Heroic boss every N waves, later";

	public const string HeroicBossLateIntervalDescription =
		"Waves between heroic boss waves from the switch wave on.";

	public const string HeroicBossesPerBossWaveLabel = "Extra heroic bosses per boss wave";

	public const string HeroicBossesPerBossWaveDescription =
		"Each heroic boss wave brings this many more than the last. Defaults: 1 at 50, 2 at 100, 3 at 125, " +
		"4 at 150.";

	public const string MaxHeroicBossesLabel = "Max heroic bosses per wave";

	public const string MaxHeroicBossesDescription = "Heroic boss cap per wave.";

	public const string HeroicBossEarlyFromWaveLabel = "Heroic bosses may come early from wave";

	public const string HeroicBossEarlyFromWaveDescription =
		"From this wave each heroic boss can enter with the wave's first spawns instead of after the last monster.";

	public const string HeroicBossEarlyChanceLabel = "Heroic boss early chance (%)";

	public const string HeroicBossEarlyChanceDescription =
		"Chance per heroic boss to enter first. 0 = always last, 100 = always first.";

	public const string HeroicBossDelayLabel = "Heroic boss entrance delay (s)";

	public const string HeroicBossDelayDescription =
		"Seconds after the wave's last spawn before the boss enters.";

	public const string HeroicBossAbilityHasteLabel = "Heroic boss ability haste";

	public const string HeroicBossAbilityHasteDescription =
		"Bonus ability haste. Cooldowns x 1 / (1 + haste / 100), down to the game's floor. 100 = about 2x " +
		"casts, 0 = off.";

	public const string HeroicBossHasteGrowthLabel = "Heroic boss haste per later boss wave";

	public const string HeroicBossHasteGrowthDescription =
		"Haste added per heroic boss wave after the first. 200 + 50: 200 at 49, 250 at 99, 300 at 124, " +
		"350 at 149. 0 = flat.";

	public const string HeroicBossHealthMultiplierLabel = "Heroic boss health multiplier";

	public const string HeroicBossHealthMultiplierDescription =
		"Heroic boss HP x this. Stacks with world, difficulty, co-op and wave scaling.";

	public const string HeroicBossDamageMultiplierLabel = "Heroic boss damage multiplier";

	public const string HeroicBossDamageMultiplierDescription =
		"Heroic boss damage x this. Stacks with world, difficulty and wave scaling.";

	public const string HeroicFastKillSecondsLabel = "Heroic fast kill: seconds per boss";

	public const string HeroicFastKillSecondsDescription =
		"After a boss wave, the heroic bosses' best burst of damage is turned into a kill time per boss. " +
		"Under this, later heroic bosses get more health in proportion: a 30 s kill at 90 gives x3. Never " +
		"goes back down. 0 = off, for parties who want to one-shot bosses.";

	public const string HeroicFastKillBurstLabel = "Heroic fast kill: burst window (s)";

	public const string HeroicFastKillBurstDescription =
		"The heaviest stretch of this many seconds of damage on the heroic bosses is what counts, so stalling " +
		"or a boss's immune phase doesn't hide a fast kill.";

	public const string HeroicFastKillMaxStepLabel = "Heroic fast kill: largest step";

	public const string HeroicFastKillMaxStepDescription =
		"Most one fast kill can multiply heroic boss health by, so a single burst doesn't overshoot.";

	public const string HeroicBossTypesLabel = "Heroic boss types";

	public const string HeroicBossTypesDescription =
		"Random pool. None checked = every boss. Applies next arena.";

	public const string HeroicBossChaosLabel = "Heroic boss Corrupted Chaos rewards";

	public const string HeroicBossChaosDescription =
		"Corrupted Chaos shrines (Pain kind) per heroic boss kill.";

	public const string HeroicBossStardustLabel = "Heroic boss stardust";

	public const string HeroicBossStardustDescription =
		"A heroic boss drops the stardust a boss kill gives in a normal run: 16, plus 2 / 4 / 7 on Normal / " +
		"Hard / Nightmare, plus 3 per evil lucid dream. Every player gets it.";

	public const string ItemRarityFloorLabel = "Rarity floor with item multipliers";

	public const string ItemRarityFloorDescription =
		"When an item multiplier pushes chances past 100%, the excess comes off the lowest rarities " +
		"first, down to this floor. Only cuts, never raises.";

	public const string MaxKillGoldLabel = "Max kill gold per player";

	public const string MaxKillGoldDescription =
		"Kill gold cap per player per kill, before gold-gain bonuses. Miniboss, Pain and heroic boss bonus " +
		"gold is uncapped. 0 = no cap (vanilla kill gold).";

	public const string HeroicBossDropMultiplierLabel = "Heroic boss item chance multiplier";

	public const string HeroicBossDreamDustLabel = "Heroic boss dream dust";

	public const string HeroicBossGoldLabel = "Heroic boss bonus gold";

	public const string DreamWellLabel = "Dream Well";

	public const string DreamWellDescription =
		"Dream Well on the edge ring. Unlimited upgrades for dream dust.";

	public const string AltarOfCleansingLabel = "Altar of Cleansing";

	public const string AltarOfCleansingDescription =
		"Altar of Cleansing on the edge ring. Unlimited. Resets a memory or essence to its minimum level " +
		"or quality for gold and refunds the dream dust.";

	public const string ShrineOfAscensionLabel = "Shrine of Ascension";

	public const string ShrineOfAscensionDescription =
		"Shrine of Ascension on the edge ring. +1 rarity, keeps level or quality. 4 uses per player. " +
		"Vanilla price (30 to 150 gold by rarity, +10% per use) unless gold shrines are free.";

	public const string AscensionResetEveryWavesLabel = "Ascension uses come back every N waves";

	public const string AscensionResetEveryWavesDescription =
		"Locked until the first of these waves is cleared. After that, everyone's 4 uses reset every N " +
		"cleared waves (1 = every wave, 10 = 10, 20, 30…). Unused uses don't carry over.";

	public const string MawOfDoomLabel = "Maw of Doom";

	public const string MawOfDoomDescription =
		"Maw of Doom on the edge ring. Levels a memory or raises essence quality for gold. Flat 20 gold (0 " +
		"with free gold shrines) and 8% destroy chance (5% for essences) on every use.";

	public const string ShrineOfDestinyLabel = "Shrine of Destiny";

	public const string ShrineOfDestinyDescription =
		"Shrine of Destiny on the edge ring. Sacrifice essence quality for an essence of the same rarity " +
		"or better. Rerolls cost more quality each time. Claims are free.";

	public const string DestinyClaimsPerWaveLabel = "Shrine of Destiny claims per wave";

	public const string DestinyClaimsPerWaveDescription =
		"Essence claims per player per wave. Resets when the wave is cleared.";

	public const string HatredRemnantLabel = "Hatred Remnant";

	public const string HatredRemnantDescription =
		"Hatred Remnant on the edge ring. Pick a strength: a curse plus a matching reward shard. Once per " +
		"player per wave.";

	public const string HatredDreamDustPerWaveLabel = "Hatred Shard dream dust per wave";

	public const string HatredDreamDustPerWaveDescription =
		"Shard dream dust = this x wave x strength (None 0.8, Mild 1.5, Potent 2.4, Powerful 3.3), " +
		"+/-15%.";

	public const string ReminiscenceOfLuckLabel = "Reminiscence of Luck";

	public const string ReminiscenceOfLuckDescription =
		"Reminiscence of Luck on the edge ring. One free memory or essence per player per wave from " +
		"personal choices. Pray for gold to add a choice, up to 3 times. Resets when the wave is cleared.";

	public const string HatredGoldPerWaveLabel = "Hatred Shard gold per wave";

	public const string HatredGoldPerWaveDescription =
		"Shard gold = this x wave x strength (None 0.5, Mild 0.8, Potent 1.2, Powerful 1.6), +/-15%. " +
		"0 = shards never roll gold.";

	public const string CurseMildWavesLabel = "Mild curse length (waves of kills)";

	public const string CursePotentWavesLabel = "Potent curse length (waves of kills)";

	public const string CursePowerfulWavesLabel = "Powerful curse length (waves of kills)";

	public const string CurseWavesDescription =
		"Curses end on kills, not travel. Length in waves of monsters at full strength: 1 = a whole wave, " +
		"not split across players. Assists count, heroic bosses count 10.";

	public const string StartingGoldLabel = "Starting gold";

	public const string StartingGoldDescription =
		"Bonus gold per player at run start. Mid-run joiners get the game's catch-up gold. 0 = off.";

	public const string StartingDreamDustLabel = "Starting dream dust";

	public const string StartingDreamDustDescription =
		"Bonus dream dust per player at run start: amount x 2^(starting world), so 10 gives 20 in world 1 and " +
		"80 in world 3. 0 = off.";

	public const string GoldPriceGrowthPercentLabel = "Gold price growth (%)";

	public const string GoldPriceGrowthPercentDescription =
		"Gold prices go up this much every N waves, compounding: 5% every wave is 11.5x at wave 50 and 131x " +
		"at wave 100. Clients see the same prices. Altar of Cleansing stays vanilla. 0 = off.";

	public const string GoldPriceGrowthEveryWavesLabel = "Gold prices grow every N waves";

	public const string GoldPriceGrowthEveryWavesDescription =
		"How often gold prices take a step up. The first step is at wave N.";

	public const string GoldPriceGrowthUntilWaveLabel = "Gold prices stop growing at wave";

	public const string GoldPriceGrowthUntilWaveDescription =
		"Last wave that raises gold prices; they stay there after it. 100 with 5% = 131x. 0 = never stop.";

	public const string FreeGoldShrinesLabel = "Free gold shrines";

	public const string FreeGoldShrinesDescription =
		"Gold prices set to 0 in the arena: free Maw of Doom, Ascension and Luck prays, clients included. Maw " +
		"keeps its 8% risk. Altar of Cleansing still costs gold.";

	public const string CheatCommandsLabel = "Allow cheat commands";

	public const string CheatCommandsDescription =
		"Enables laststand_gold (gold for everyone) and laststand_testboss (weak heroic boss).";

	public const string ArenaRoomLabel = "Arena room";

	public const string ArenaRoomDescription = "Boss room used as the arena. Applies next run.";

	public const string FirstWaveDelayLabel = "First wave delay (s)";

	public const string FirstWaveDelayDescription = "Seconds from arrival to wave 1.";

	public const string BossIntermissionLabel = "Break after a boss wave (s)";

	public const string BossIntermissionDescription = "Break after a miniboss, Pain or heroic boss wave.";

	public const string CurseToStartLabel = "Curse to start each wave";

	public const string CurseToStartDescription =
		"Difficulty: no wave timer. A wave starts only after a player picks a curse (Mild or stronger) at the " +
		"Hatred Remnant, which curses every other player too, each with a different curse. Turns the Hatred " +
		"Remnant on.";

	public const string CurseStartDelayLabel = "Curse to start: delay (s)";

	public const string CurseStartDelayDescription = "Seconds from the curse to the wave.";

	public const string IntermissionLabel = "Break between waves (s)";

	public const string IntermissionDescription = "Break after a wave is cleared (every monster dead).";

	public const string RegularBaseLabel = "Monsters in wave 1";

	public const string RegularBaseDescription = "Regular monsters in wave 1, solo.";

	public const string RegularGrowthLabel = "Extra monsters per wave";

	public const string RegularGrowthDescription = "Regular monsters added per wave.";

	public const string SurgeIntervalLabel = "Surge every N waves";

	public const string SurgeIntervalDescription =
		"Every Nth wave multiplies regular monsters, hunters and the alive cap. Once waves merge, the count " +
		"makes them stronger instead. Bosses unaffected.";

	public const string SurgeMultiplierLabel = "Surge monster multiplier";

	public const string SurgeMultiplierDescription = "Surge multiplier. 1 = no surges.";

	public const string MergedWaveSizeLabel = "Merged wave size";

	public const string MergedWaveSizeDescription =
		"From the wave where wave 1's monsters plus the growth reach this (wave 48 by default), every wave " +
		"spawns at most this many monsters and hunters, whatever the player count, and they carry the rest's " +
		"strength: health, damage, bonus gold and dream dust x count / size. 0 = off.";

	public const string MergedSurgeMiniBossesLabel = "Merged surges: extra minibosses";

	public const string MergedSurgeMiniBossesDescription =
		"Minibosses added to every surge wave once waves merge, on top of any scheduled ones.";

	public const string MergedSurgePainLabel = "Merged surges: extra Pain minibosses";

	public const string MergedSurgePainDescription =
		"Pain minibosses added to every surge wave once waves merge, on top of any scheduled ones.";

	public const string MergedMiniBossHealthLabel = "Merged waves: miniboss health (x toughest monster)";

	public const string MergedMiniBossHealthDescription =
		"Once waves merge, minibosses and Pain minibosses have at least this many times the max health of the " +
		"toughest monster in the wave. 0 = off.";

	public const string MergedHeroicBossHealthLabel = "Merged waves: heroic boss health (x toughest monster)";

	public const string MergedHeroicBossHealthDescription =
		"Once waves merge, heroic bosses have at least this many times the max health of the toughest monster " +
		"in the wave. 0 = off.";

	public const string ExtraPlayerShareLabel = "Extra player share";

	public const string ExtraPlayerShareDescription =
		"Wave size added per extra player. 0.5 = 1.5x for two players.";

	public const string MaxAliveBaseLabel = "Max monsters alive";

	public const string MaxAliveBaseDescription = "Alive cap. Extra spawns queue. Keeps host FPS up.";

	public const string MaxAlivePerPlayerLabel = "Max alive per extra player";

	public const string MaxAlivePerPlayerDescription =
		"Added to the cap per extra player. 0 = same cap for everyone.";

	public const string SpawnIntervalLabel = "Spawn interval (s)";

	public const string SpawnIntervalDescription = "Seconds between spawn batches.";

	public const string SpawnsPerIntervalLabel = "Spawns per batch";

	public const string SpawnsPerIntervalDescription = "Monsters per batch.";

	public const string MinSpawnDistanceLabel = "Min spawn distance from heroes (m)";

	public const string MinSpawnDistanceDescription =
		"Monsters spawn anywhere in the room, never closer than this to a hero.";

	public const string HealthPerWaveLabel = "Health per wave (%)";

	public const string HealthPerWaveDescription = "Max health per wave, stacking. 10 = +100% at wave 11.";

	public const string DamagePerWaveLabel = "Damage per wave (%)";

	public const string DamagePerWaveDescription = "Attack and ability damage per wave, stacking.";

	public const string LateScalingFromWaveLabel = "Steeper scaling from wave";

	public const string LateScalingFromWaveDescription =
		"From this wave, each wave adds the late values below instead. Earlier bonuses stay.";

	public const string FastClearKillPercentLabel = "Fast clear: instant kills (%)";

	public const string FastClearKillPercentDescription =
		"When this share of a wave's monsters is killed instantly (see below), every later wave is tougher. " +
		"0 = off.";

	public const string FastClearFromWaveLabel = "Fast clear: from wave";

	public const string FastClearFromWaveDescription =
		"First wave that can count as a fast clear. Earlier waves never raise stats.";

	public const string FastClearKillSecondsLabel = "Fast clear: instant within (s)";

	public const string FastClearKillSecondsDescription =
		"A monster counts as killed instantly when it dies within this long of the first hit it takes. 0 = off.";

	public const string FastClearStatMultiplierLabel = "Fast clear: stats from next wave (x)";

	public const string FastClearStatMultiplierDescription =
		"Health and damage multiplier from the next wave on, stacking with each fast clear, on top of wave " +
		"scaling. 1 = off.";

	public const string FastClearHeroicBossesLabel = "Fast clear: heroic bosses too";

	public const string FastClearHeroicBossesDescription =
		"Heroic bosses also get tougher from fast clears, by the share below.";

	public const string FastClearHeroicBossPercentLabel = "Fast clear: heroic boss share (%)";

	public const string FastClearHeroicBossPercentDescription =
		"Each fast clear counts this much for heroic bosses. At 50% with x2, 10 fast clears give x32 instead " +
		"of x1024.";

	public const string LateHealthPerWaveLabel = "Late health per wave (%)";

	public const string LateHealthPerWaveDescription =
		"Max health per wave from the steeper-scaling wave on.";

	public const string LateDamagePerWaveLabel = "Late damage per wave (%)";

	public const string LateDamagePerWaveDescription =
		"Attack and ability damage per wave from the steeper-scaling wave on.";

	public const string StartingAmbientLevelLabel = "Starting monster level";

	public const string StartingAmbientLevelDescription = "Ambient level (monster spawn level) at wave 1.";

	public const string StartingWorldLabel = "Starting world";

	public const string StartingWorldDescription =
		"Which world the run plays as. Higher = tougher monsters, better loot, more gold and XP, pricier " +
		"shrines.";

	public const string WavesPerAmbientLevelLabel = "Waves per monster level";

	public const string WavesPerAmbientLevelDescription = "Ambient level +1 every N waves.";

	public const string MiniBossFromWaveLabel = "First miniboss wave";

	public const string MiniBossFromWaveDescription = "First miniboss wave.";

	public const string MiniBossIntervalLabel = "Waves between miniboss waves";

	public const string MiniBossIntervalDescription = "Miniboss wave every N waves.";

	public const string MiniBossOccurrencesPerExtraLabel = "Miniboss waves per extra miniboss";

	public const string PainOccurrencesPerExtraLabel = "Pain waves per extra Pain miniboss";

	public const string OccurrencesPerExtraDescription =
		"+1 boss after this many of these waves. 2 = 1, 1, 2, 2, 3…";

	public const string MaxMiniBossesLabel = "Max minibosses per wave";

	public const string MaxMiniBossesDescription = "Miniboss cap per wave.";

	public const string PainFromWaveLabel = "First Pain wave";

	public const string PainFromWaveDescription = "First Pain wave.";

	public const string PainIntervalLabel = "Waves between Pain waves";

	public const string PainIntervalDescription = "Pain wave every N waves.";

	public const string MaxPainLabel = "Max Pain minibosses per wave";

	public const string MaxPainDescription = "Pain miniboss cap per wave.";

	public const string PainDamageLabel = "Pain damage (% per s)";

	public const string PainDamageDescription =
		"Max health + shield every hero loses per second, per living Pain miniboss. Stops at 1 HP.";

	public const string PainHealthBonusLabel = "Pain health bonus (%)";

	public const string PainHealthBonusDescription = "Bonus max health on Pain minibosses.";

	public const string PainDamageBonusLabel = "Pain damage bonus (%)";

	public const string PainDamageBonusDescription = "Bonus attack and ability damage on Pain minibosses.";

	public const string ForeignFromWaveLabel = "First new monster wave";

	public const string ForeignFromWaveDescription = "First wave that adds monsters from other worlds.";

	public const string ForeignIntervalLabel = "Waves between new monsters";

	public const string ForeignIntervalDescription = "New monster types every N waves, weakest worlds first.";

	public const string ForeignPerUnlockLabel = "New monster types per step";

	public const string ForeignPerUnlockDescription = "Types added per step.";

	public const string HunterFromWaveLabel = "First hunter wave";

	public const string HunterFromWaveDescription =
		"Hunters start spawning. Waves without an environment become hunter zones.";

	public const string AllHuntersFromWaveLabel = "Every monster a hunter from wave";

	public const string AllHuntersFromWaveDescription =
		"Hunter share grows every wave until every regular monster is a hunter at this wave.";

	public const string HuntLevelStepLabel = "Waves per hunt level";

	public const string HuntLevelStepDescription =
		"Hunter zones rain the game's artillery around heroes until the wave is cleared. Hunt level " +
		"starts at 1, +1 every N waves.";

	public const string MaxHuntLevelLabel = "Max hunt level";

	public const string MaxHuntLevelDescription =
		"Hunt level cap. 0 = no cap. Higher = stronger artillery and hunters (the HUD shows at most 5).";

	public const string MirageFromWaveLabel = "First Mirage wave";

	public const string MirageFromWaveDescription = "First wave with Mirage skins.";

	public const string MirageIntervalLabel = "Waves per new Mirage skin";

	public const string MirageIntervalDescription =
		"Next stronger skin unlocks every N waves, weakest first.";

	public const string MirageChanceLabel = "Mirage chance (%)";

	public const string MirageChanceDescription =
		"Chance a regular monster or hunter spawns with an unlocked skin.";

	public const string MirageRampFromWaveLabel = "Mirage chance grows from wave";

	public const string MirageRampFromWaveDescription = "Mirage chance climbs every wave from here to 100%.";

	public const string AllMirageFromWaveLabel = "Every monster a Mirage from wave";

	public const string AllMirageFromWaveDescription =
		"Every regular monster and hunter has a skin. Lesser monsters never do.";

	public const string MonsterItemDropsLabel = "Monster item drops";

	public const string MonsterItemDropsDescription =
		"Monsters drop memories and essences by the chances below. Off = Reminiscence of Luck is the item " +
		"source. Dust and bonus gold drop either way.";

	public const string MobItemsPerPlayerLabel = "Monster items per player per wave";

	public const string MobItemsPerPlayerDescription =
		"Monster item cap per player per wave. 1 = solo gets 1, two players get 1 memory + 1 essence. " +
		"Minibosses uncapped.";

	public const string FreeLeftoverItemsLabel = "Free leftover items at wave end";

	public const string FreeLeftoverItemsDescription =
		"Items drop reserved for one player. Unclaimed ones become free for everyone when the wave ends.";

	public const string GuaranteedDropLabel = "Guaranteed drop per wave";

	public const string GuaranteedDropDescription =
		"Players with no item this wave get one when it's cleared.";

	public const string RewardGrowthLabel = "Gold growth per wave (%)";

	public const string RewardGrowthDescription = "Bonus gold +% per wave. Dream dust stays flat.";

	public const string CommonDropLabel = "Common item chance (%)";

	public const string RareDropLabel = "Rare item chance (%)";

	public const string EpicDropLabel = "Epic item chance (%)";

	public const string LegendaryDropLabel = "Legendary item chance (%)";

	public const string RarityDropDescription =
		"Per-kill drop chance at this rarity. Max one item per kill. Defaults leave 60% of kills empty.";

	public const string MemoryShareLabel = "Memory share (%)";

	public const string MemoryShareDescription =
		"Memory vs essence chance for solo and minibosses. In co-op, monster drops alternate.";

	public const string RegularDropMultiplierLabel = "Regular: item chance multiplier";

	public const string MiniBossDropMultiplierLabel = "Miniboss: item chance multiplier";

	public const string PainDropMultiplierLabel = "Pain: item chance multiplier";

	public const string DropMultiplierDescription =
		"Multiplies every rarity chance for this tier. Rolled rarest first; past 100%, common gets cut.";

	public const string ChaosLegendaryLabel = "Chaos reward: legendary chance (%)";

	public const string ChaosEpicLabel = "Chaos reward: epic chance (%)";

	public const string ChaosRareLabel = "Chaos reward: rare chance (%)";

	public const string ChaosRarityDescription =
		"Miniboss chaos reward rarity, common otherwise. Replaces the game's roll (almost always " +
		"legendary).";

	public const string ItemTargetWaveLabel = "Item chances reach target at wave";

	public const string ItemTargetWaveDescription =
		"Item chances go linearly from wave 1 values to target values by this wave, then hold.";

	public const string CommonAtTargetLabel = "Common chance at target wave (%)";

	public const string RareAtTargetLabel = "Rare chance at target wave (%)";

	public const string EpicAtTargetLabel = "Epic chance at target wave (%)";

	public const string LegendaryAtTargetLabel = "Legendary chance at target wave (%)";

	public const string ItemAtTargetDescription =
		"Per-kill chance at this rarity at the target wave. Summing to 100% = every kill rolls an item " +
		"(still capped per player).";

	public const string ChaosTargetWaveLabel = "Chaos chances reach target at wave";

	public const string ChaosTargetWaveDescription =
		"Chaos chances go linearly from wave 1 values to target values by this wave, then hold.";

	public const string ChaosLegendaryAtTargetLabel = "Chaos reward: legendary at target wave (%)";

	public const string ChaosEpicAtTargetLabel = "Chaos reward: epic at target wave (%)";

	public const string ChaosRareAtTargetLabel = "Chaos reward: rare at target wave (%)";

	public const string ChaosAtTargetDescription =
		"Chance at this rarity at the target wave. Common gets the rest.";

	public const string RegularDreamDustLabel = "Regular: dream dust";

	public const string RegularGoldLabel = "Regular: bonus gold";

	public const string MiniBossDreamDustLabel = "Miniboss: dream dust";

	public const string MiniBossGoldLabel = "Miniboss: bonus gold";

	public const string PainDreamDustLabel = "Pain: dream dust";

	public const string PainGoldLabel = "Pain: bonus gold";

	public const string DreamDustDescription = "Dream dust per kill of this tier. Flat.";

	public const string GoldDescription = "Bonus gold per kill of this tier at wave 1, on top of kill gold.";

	public const string FriendlyFireFromWaveLabel = "First dark urge wave";

	public const string FriendlyFireFromWaveDescription =
		"From this wave, waves without a heroic boss can curse every player with Dark Urge until cleared: " +
		"friendly fire on. Co-op only.";

	public const string FriendlyFireChanceLabel = "Dark urge chance (%)";

	public const string FriendlyFireChanceDescription = "Chance per eligible wave.";

	public const string DarkUrgeStrengthLabel = "Dark urge curse strength";

	public const string DarkUrgeStrengthDescription =
		"Damage between players (35% of that against a player under half health).";

	public const string MildDarkUrge = "Mild: 15% damage between players";

	public const string PotentDarkUrge = "Potent: 25% damage between players";

	public const string PowerfulDarkUrge = "Powerful: 40% damage between players";

	public const string EnvironmentChanceLabel = "Environment chance (%)";

	public const string EnvironmentChanceDescription =
		"Chance per wave of an environment before the higher chance starts: a random room effect that lasts " +
		"until the wave is cleared. Never on Pain waves.";

	public const string EnvironmentHighFromWaveLabel = "Higher environment chance from wave";

	public const string EnvironmentHighFromWaveDescription =
		"From this wave until the first hunter wave, the higher chance replaces the first one.";

	public const string EnvironmentHighChanceLabel = "Higher environment chance (%)";

	public const string EnvironmentHighChanceDescription = "Chance per wave from the wave above. 100 = every wave.";

	public const string EnvironmentHuntedChanceLabel = "Hunter zone environment chance (%)";

	public const string EnvironmentHuntedChanceDescription =
		"Chance per wave from the first hunter wave on. The environment replaces that wave's hunter zone.";

	public const string EnvironmentHuntedMaxPerBlockLabel = "Hunter zone: max environments per block";

	public const string EnvironmentHuntedMaxPerBlockDescription =
		"Most environment waves per block, so the rest keep their hunter zone.";

	public const string EnvironmentHuntedBlockWavesLabel = "Hunter zone: block size (waves)";

	public const string EnvironmentHuntedBlockWavesDescription =
		"Blocks start at the first hunter wave: 100-109, 110-119 … by default.";

	public const string EnvironmentOnHeroicFromWaveLabel = "Environments on heroic boss waves from wave";

	public const string EnvironmentOnHeroicFromWaveDescription =
		"Before this wave, heroic boss waves never get an environment.";

	public const string EnvironmentUntilWaveLabel = "Last environment wave";

	public const string EnvironmentUntilWaveDescription =
		"Last environment wave. 0 = never stop. Replaces the hunter zone on its wave (hunters still " +
		"spawn).";

	public const string EnvironmentTypesLabel = "Environment types";

	public const string EnvironmentTypesDescription = "Random pool. None checked = no environments.";

	public const string AcceleratedTimeDetail = "everyone faster";

	public const string UnstableVeilOfTimeDetail = "one memory fast, the rest slow";

	public const string GoldEverywhereDetail = "gold from kills";

	public const string PureDreamDetail = "dream dust from kills";

	public const string LingeringAuraOfGuidanceDetail = "health regen, leaves shrines behind";

	public const string StarCookieDetail = "cookies, leaves shrines behind";

	public const string BlackRainDetail = "elements weaker";

	public const string SymbioteHabitatDetail = "attack speed for armor";

	public const string InversionSigilDetail = "elements flipped";

	public const string ToxicAreaDetail = "weaker healing and shields";

	public const string GravityTrainingDetail = "everyone slower";

	public const string ArcticTerritoryDetail = "icy mist";

	public const string EngulfedInFlameDetail = "fire near monsters";

	public const string DarkCondensationZoneDetail = "half vision";

	public const string VeilOfDarkDetail = "patches of darkness";

	public const string WarpingFieldDetail = "random teleports";

	public const string RiskOfMeteorsDetail = "meteor showers";

	public const string FireDevilDetail = "fire tornadoes";

	public const string InkStrikeWarningDetail = "slowing ink drops";

	public const string GazeOfErebosDetail = "abyss gazes";

	public const string UnstableRatSwarmDetail = "exploding rats";

	public const string LeafPuppiesDetail = "monsters of random size";

	public const string MeteoricLifeDetail = "random sizes, exploding corpses";

}
