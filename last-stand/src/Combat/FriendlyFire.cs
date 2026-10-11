using System.Collections.Generic;
using LastStand.Config;
using Mirror;

namespace LastStand.Combat;

internal sealed class FriendlyFire
{
	private readonly List<Se_Curse_DarkUrge> _curses = new List<Se_Curse_DarkUrge>();
	private readonly List<Hero> _guardedHeroes = new List<Hero>();
	private HatredStrengthType _strength;
	private double _nextRefreshAt;

	public bool IsActive { get; private set; }

	public void Begin(HatredStrengthType strength, double now)
	{
		End();
		IsActive = true;
		_strength = strength;
		Refresh(now);
	}

	public void Tick(double now)
	{
		if (IsActive && now >= _nextRefreshAt)
		{
			Refresh(now);
		}
	}

	public void End()
	{
		IsActive = false;
		foreach (Hero hero in _guardedHeroes)
		{
			if (hero != null)
			{
				hero.takenDamageProcessor.Remove(CountUrgeOnce);
			}
		}

		_guardedHeroes.Clear();
		if (NetworkServer.active)
		{
			foreach (Se_Curse_DarkUrge curse in _curses)
			{
				if (!curse.IsNullOrInactive())
				{
					curse.Destroy();
				}
			}

			RemoveLeftoverCurses();
		}

		_curses.Clear();
	}

	private void Refresh(double now)
	{
		_nextRefreshAt = now + Tuning.FriendlyFireRefreshSeconds;
		if (!NetworkServer.active || DewPlayer.gamePlayers == null)
		{
			return;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player == null || !player.isHumanPlayer || player.hero.IsNullOrInactive())
			{
				continue;
			}

			Hero hero = player.hero;
			Guard(hero);
			if (!hero.Status.TryGetStatusEffect(out Se_Curse_DarkUrge _))
			{
				_curses.Add(hero.CreateStatusEffect<Se_Curse_DarkUrge>(hero, new CastInfo(hero, hero), Configure));
			}
		}
	}

	private void Configure(Se_Curse_DarkUrge curse)
	{
		curse.currentStrength = _strength;
		curse.progressType = QuestProgressType.Hidden;
		curse.dontRemoveOnKnockOut = true;
	}

	private void Guard(Hero hero)
	{
		if (_guardedHeroes.Contains(hero))
		{
			return;
		}

		hero.takenDamageProcessor.Add(CountUrgeOnce, Tuning.DarkUrgeGuardPriority);
		_guardedHeroes.Add(hero);
	}

	private static void CountUrgeOnce(ref DamageData data, Actor actor, Entity to)
	{
		Hero attacker = actor != null ? actor.FindFirstOfType<Hero>() : null;
		if (attacker == null || to == null || attacker == to ||
		    !attacker.Status.TryGetStatusEffect(out Se_Curse_DarkUrge attackerUrge) ||
		    !to.Status.TryGetStatusEffect(out Se_Curse_DarkUrge targetUrge) ||
		    !data.IsAmountModifiedBy(attackerUrge))
		{
			return;
		}

		data.SetAmountModifiedBy(targetUrge);
	}

	private static void RemoveLeftoverCurses()
	{
		if (DewPlayer.gamePlayers == null)
		{
			return;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player == null || player.hero.IsNullOrInactive())
			{
				continue;
			}

			if (player.hero.Status.TryGetStatusEffect(out Se_Curse_DarkUrge curse) &&
			    curse.progressType == QuestProgressType.Hidden && curse.dontRemoveOnKnockOut)
			{
				curse.Destroy();
			}
		}
	}
}
