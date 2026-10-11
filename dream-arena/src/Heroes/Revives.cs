using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Text;

namespace DreamArena.Heroes;

internal sealed class Revives
{
	private readonly Dictionary<Hero, double> _downSince = new Dictionary<Hero, double>();
	private readonly List<Hero> _done = new List<Hero>();

	public void Tick(double now, SessionSettings settings)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		_done.Clear();
		foreach (Hero hero in actors.allHeroes)
		{
			if (hero == null || !hero.isKnockedOut
			    || !hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
			{
				continue;
			}

			if (settings.NoRescueQuests && !knockedOut.disableQuest)
			{
				knockedOut.disableQuest = true;
				Log.Info(LogText.RescueQuestSuppressed(hero.name));
			}

			if (settings.AutoReviveSeconds <= 0)
			{
				continue;
			}

			if (!_downSince.TryGetValue(hero, out double since))
			{
				_downSince[hero] = now;
			}
			else if (now - since >= settings.AutoReviveSeconds)
			{
				knockedOut.Revive(settings.ReviveHealthRatio);
				_done.Add(hero);
				Log.Info(LogText.HeroRevived(hero.name, settings.ReviveHealthRatio));
			}
		}

		foreach (KeyValuePair<Hero, double> entry in _downSince)
		{
			if (entry.Key == null || !entry.Key.isKnockedOut)
			{
				_done.Add(entry.Key);
			}
		}

		foreach (Hero hero in _done)
		{
			_downSince.Remove(hero);
		}

		_done.Clear();
	}

	public static int ReviveAll()
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		int revived = 0;
		foreach (Hero hero in actors.allHeroes)
		{
			if (hero != null && hero.isKnockedOut && hero.Status.TryGetStatusEffect(out Se_HeroKnockedOut knockedOut))
			{
				knockedOut.Revive(Tuning.FullHealthRatio);
				revived++;
			}
		}

		return revived;
	}

	public void Reset()
	{
		_downSince.Clear();
		_done.Clear();
	}
}
