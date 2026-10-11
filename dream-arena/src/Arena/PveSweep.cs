using System.Collections.Generic;
using DreamArena.Config;
using DreamArena.Diagnostics;
using DreamArena.Text;

namespace DreamArena.Arena;

internal sealed class PveSweep
{
	private readonly List<Actor> _doomed = new List<Actor>();
	private double _nextSweepAt;

	public void Tick(double now, SessionSettings settings)
	{
		if (!settings.NoMonsters || now < _nextSweepAt)
		{
			return;
		}

		_nextSweepAt = now + Tuning.PveSweepSeconds;
		Sweep(settings);
	}

	public int Sweep(SessionSettings settings)
	{
		ActorManager actors = NetworkedManagerBase<ActorManager>.softInstance;
		if (actors == null)
		{
			return 0;
		}

		_doomed.Clear();
		foreach (Entity entity in actors.allEntities)
		{
			if (!entity.IsNullOrInactive() && IsPve(entity))
			{
				_doomed.Add(entity);
			}
		}

		foreach (Actor actor in actors.allActors)
		{
			if (!actor.IsNullOrInactive() && !(actor is Entity) && IsUnwanted(actor, settings))
			{
				_doomed.Add(actor);
			}
		}

		foreach (Actor actor in _doomed)
		{
			actor.Destroy();
		}

		int count = _doomed.Count;
		_doomed.Clear();
		if (count > 0)
		{
			Log.Info(LogText.PveSwept(count));
		}

		return count;
	}

	public void Reset()
	{
		_doomed.Clear();
		_nextSweepAt = 0;
	}

	private static bool IsPve(Entity entity)
	{
		if (entity is Hero)
		{
			return false;
		}

		if (entity is Summon)
		{
			return entity.owner == null || !entity.owner.isHumanPlayer;
		}

		return entity is Monster || entity is PropEntity;
	}

	private static bool IsUnwanted(Actor actor, SessionSettings settings)
	{
		if (actor is Rift)
		{
			return false;
		}

		if (actor is GameModifierBase)
		{
			return settings.RemoveGameModifiers;
		}

		if (actor is Shrine || actor is Treasure || actor is Artifact || actor is Room_Trap_Toggleable)
		{
			return true;
		}

		return actor is IItem item && item.handOwner == null && item.owner == null && item.tempOwner == null;
	}
}
