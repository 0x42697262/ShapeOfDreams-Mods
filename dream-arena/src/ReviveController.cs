using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Puts knocked-out players back on their feet after a delay.
/// </summary>
/// <remarks>
/// Without this a free-for-all ends after one death each, since a knocked-out
/// hero never recovers on its own and this mod deliberately blocks the run
/// from concluding.
///
/// <c>Se_HeroKnockedOut</c> is the status effect applied on knockout, and its
/// public <c>Revive(float reviveHealthMult)</c> sets health and destroys the
/// effect. It is found on the hero through
/// <c>EntityStatus.GetStatusEffect&lt;T&gt;()</c>.
///
/// Server-side only; the revive replicates like any other state change, so
/// clients need no mod.
/// </remarks>
internal sealed class ReviveController
{
	/// <summary>How long each knocked-out hero has been waiting.</summary>
	private readonly Dictionary<Hero, float> _downSince = new Dictionary<Hero, float>();

	private readonly List<Hero> _finished = new List<Hero>();

	public void Tick(float deltaTime, DreamArenaConfig config)
	{
		if (!NetworkServer.active || config == null || !config.arenaEnabled)
		{
			if (_downSince.Count > 0)
			{
				_downSince.Clear();
			}

			return;
		}

		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return;
		}

		_finished.Clear();

		foreach (Hero hero in actors.allHeroes)
		{
			if (hero == null || !hero.isKnockedOut)
			{
				continue;
			}

			SuppressKnockoutQuest(hero, config);

			if (config.autoReviveSeconds <= 0)
			{
				continue;
			}

			_downSince.TryGetValue(hero, out float elapsed);
			elapsed += deltaTime;
			_downSince[hero] = elapsed;

			if (elapsed >= config.autoReviveSeconds)
			{
				_finished.Add(hero);
			}
		}

		foreach (Hero hero in _finished)
		{
			_downSince.Remove(hero);
			TryRevive(hero, config);
		}

		// Drop entries for heroes that got up some other way, or vanished.
		PruneRecovered(actors);
	}

	/// <summary>
	/// Stops a knockout from generating a rescue quest.
	/// </summary>
	/// <remarks>
	/// A second after any hero goes down, <c>Se_HeroKnockedOut</c> starts a
	/// <c>Quest_LostSoul</c> and, through <c>CheckAndAddHeroSoul</c>, drops a
	/// <c>RoomMod_HeroSoul</c> modifier onto a node. In a PvP match knockouts
	/// are constant, so that would mean a stream of rescue quests and modifiers
	/// pushed back onto nodes this mod had already stripped.
	///
	/// Both are gated behind the effect's own public <c>disableQuest</c> flag,
	/// so setting it prevents them outright rather than just hiding them. The
	/// per-frame scan sets it well inside the one-second window.
	/// </remarks>
	private static void SuppressKnockoutQuest(Hero hero, DreamArenaConfig config)
	{
		if (!config.suppressKnockoutQuests)
		{
			return;
		}

		Se_HeroKnockedOut knockedOut = hero.Status?.GetStatusEffect<Se_HeroKnockedOut>();
		if (knockedOut == null || knockedOut.disableQuest)
		{
			return;
		}

		knockedOut.disableQuest = true;
		Debug.Log($"{ModIdentity.LogPrefix}suppressed the rescue quest for {hero.name}.");
	}

	private static void TryRevive(Hero hero, DreamArenaConfig config)
	{
		if (hero == null || !hero.isKnockedOut)
		{
			return;
		}

		Se_HeroKnockedOut knockedOut = hero.Status?.GetStatusEffect<Se_HeroKnockedOut>();
		if (knockedOut == null)
		{
			Debug.LogWarning($"{ModIdentity.LogPrefix}{hero.name} is knocked out but has no " +
			                 "Se_HeroKnockedOut effect; cannot revive it.");
			return;
		}

		float healthMultiplier = Mathf.Clamp(config.reviveHealthPercent, 1, 100) / 100f;
		knockedOut.Revive(healthMultiplier);

		Debug.Log($"{ModIdentity.LogPrefix}revived {hero.name} at " +
		          $"{config.reviveHealthPercent}% health.");
	}

	private void PruneRecovered(ActorManager actors)
	{
		if (_downSince.Count == 0)
		{
			return;
		}

		_finished.Clear();

		foreach (KeyValuePair<Hero, float> entry in _downSince)
		{
			if (entry.Key == null || !entry.Key.isKnockedOut)
			{
				_finished.Add(entry.Key);
			}
		}

		foreach (Hero hero in _finished)
		{
			_downSince.Remove(hero);
		}
	}

	public void Reset()
	{
		_downSince.Clear();
	}
}
