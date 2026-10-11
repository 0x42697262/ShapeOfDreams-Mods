using System;
using System.Collections.Generic;
using System.Linq;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Combat;

internal sealed class MonsterCatalog
{
	private readonly List<Monster> _foreign = new List<Monster>();
	private readonly List<Monster> _zoneMiniBosses = new List<Monster>();
	private readonly List<Monster> _miniBossChoices = new List<Monster>();
	private readonly List<MiniBossEffect> _miniBossEffects = new List<MiniBossEffect>();
	private readonly List<MirageSkinEffect> _mirageSkins = new List<MirageSkinEffect>();
	private readonly List<BossMonster> _heroicBosses = new List<BossMonster>();
	private IEnumerator<Monster> _zoneRegulars;
	private int _zoneTypeCount;

	public bool IsReady => _zoneRegulars != null;

	public int ForeignTypeCount => _foreign.Count;

	public int MirageSkinCount => _mirageSkins.Count;

	public int HeroicBossCount => _heroicBosses.Count;

	public bool TryBuild(IReadOnlyList<string> heroicBossTypes, IReadOnlyList<string> allHeroicBossTypes)
	{
		Zone current = CurrentZone();
		if (current == null || current.defaultMonsters == null || current.defaultMonsters.pool == null)
		{
			return false;
		}

		MonsterPool pool = current.defaultMonsters.pool;
		HashSet<Monster> zoneTypes = new HashSet<Monster>(Monsters(pool));
		_zoneTypeCount = zoneTypes.Count;
		_zoneMiniBosses.Clear();
		_zoneMiniBosses.AddRange(zoneTypes.Where(monster => monster is ISpawnableAsMiniBoss));

		_foreign.Clear();
		foreach (Zone zone in OtherZonesWeakestFirst(current))
		{
			foreach (Monster monster in Monsters(zone.defaultMonsters.pool))
			{
				if (!zoneTypes.Contains(monster) && !_foreign.Contains(monster))
				{
					_foreign.Add(monster);
				}
			}
		}

		_miniBossEffects.Clear();
		_miniBossEffects.AddRange(DewResources.FindAllByType<MiniBossEffect>());

		_mirageSkins.Clear();
		_mirageSkins.AddRange(DewResources.FindAllByType<MirageSkinEffect>()
			.Where(skin => skin != null)
			.OrderBy(skin => skin is Se_MirageSkin_Delusion ? int.MinValue : skin.tier)
			.ThenBy(skin => skin.GetType().Name, StringComparer.Ordinal));

		_heroicBosses.Clear();
		AddHeroicBosses(heroicBossTypes);
		if (_heroicBosses.Count == 0)
		{
			Log.Info(LogText.HeroicBossesFromWholeList(heroicBossTypes.Count));
			AddHeroicBosses(allHeroicBossTypes);
		}

		_zoneRegulars = pool.GetMonsters(int.MaxValue, DewRandom.instance);
		return true;
	}

	private void AddHeroicBosses(IReadOnlyList<string> typeNames)
	{
		foreach (string typeName in typeNames)
		{
			BossMonster boss = HeroicBosses.Find(typeName);
			if (boss == null)
			{
				Log.Warn(LogText.HeroicBossUnknown(typeName));
			}
			else if (!HeroicBosses.IsSupported(boss))
			{
				Log.Warn(LogText.HeroicBossUnsupported(typeName));
			}
			else if (!_heroicBosses.Contains(boss))
			{
				_heroicBosses.Add(boss);
			}
		}
	}

	public BossMonster RandomHeroicBoss()
	{
		return _heroicBosses.Count == 0 ? null : _heroicBosses[UnityEngine.Random.Range(0, _heroicBosses.Count)];
	}

	public Monster NextRegular(int foreignUnlocked)
	{
		int foreign = Math.Min(foreignUnlocked, _foreign.Count);
		int total = _zoneTypeCount + foreign;
		if (foreign > 0 && UnityEngine.Random.Range(0, total) < foreign)
		{
			return _foreign[UnityEngine.Random.Range(0, foreign)];
		}

		return _zoneRegulars != null && _zoneRegulars.MoveNext() ? _zoneRegulars.Current : null;
	}

	public Monster RandomMiniBoss(int foreignUnlocked)
	{
		_miniBossChoices.Clear();
		_miniBossChoices.AddRange(_zoneMiniBosses);
		for (int i = 0; i < Math.Min(foreignUnlocked, _foreign.Count); i++)
		{
			if (_foreign[i] is ISpawnableAsMiniBoss)
			{
				_miniBossChoices.Add(_foreign[i]);
			}
		}

		return _miniBossChoices.Count == 0 ? null : _miniBossChoices[UnityEngine.Random.Range(0, _miniBossChoices.Count)];
	}

	public MiniBossEffect RandomMiniBossEffect()
	{
		return _miniBossEffects.Count == 0 ? null : _miniBossEffects[UnityEngine.Random.Range(0, _miniBossEffects.Count)];
	}

	public MirageSkinEffect RandomMirageSkin(int unlocked)
	{
		int count = Math.Min(unlocked, _mirageSkins.Count);
		return count <= 0 ? null : _mirageSkins[UnityEngine.Random.Range(0, count)];
	}

	public void Clear()
	{
		_foreign.Clear();
		_zoneMiniBosses.Clear();
		_miniBossChoices.Clear();
		_miniBossEffects.Clear();
		_mirageSkins.Clear();
		_heroicBosses.Clear();
		_zoneRegulars = null;
		_zoneTypeCount = 0;
	}

	private static Zone CurrentZone()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		return zones != null ? zones.currentZone : null;
	}

	private static IEnumerable<Monster> Monsters(MonsterPool pool)
	{
		if (pool == null)
		{
			yield break;
		}

		foreach (MonsterPool.SpawnRuleEntry entry in pool.GetFilteredEntries())
		{
			Monster monster = entry.monster.asset;
			if (monster != null)
			{
				yield return monster;
			}
		}
	}

	private static IEnumerable<Zone> OtherZonesWeakestFirst(Zone current)
	{
		Zone[] zones = DewResources.FindAllByNameSubstring<Zone>(GameNames.ZonePrefix).ToArray();
		zones = DewBuildProfile.current.content.FilterZones(zones);

		return zones
			.Where(zone => zone != null && zone != current && zone.name != current.name
			               && zone.defaultMonsters != null && zone.defaultMonsters.pool != null)
			.OrderBy(zone => zone.zoneTier)
			.ThenBy(zone => zone.name, StringComparer.Ordinal);
	}
}
