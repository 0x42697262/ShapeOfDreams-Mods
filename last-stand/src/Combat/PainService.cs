using System;
using LastStand.Game;
using LastStand.Text;

namespace LastStand.Combat;

internal sealed class PainService
{
	private readonly PainAura _aura = new PainAura();
	private readonly ArenaModifier _zone = new ArenaModifier();
	private readonly EnemyRegistry _enemies;
	private readonly Func<EnemyTier, bool> _hasPending;

	public PainService(EnemyRegistry enemies, Func<EnemyTier, bool> hasPending)
	{
		_enemies = enemies;
		_hasPending = hasPending;
	}

	public event Action GroupFell;

	public bool ZoneActive => _zone.IsActive;

	public void OnSpawned(Entity entity, PainSettings settings)
	{
		_aura.Empower(entity, settings);
		if (!_zone.IsActive && _zone.Add(nameof(RoomMod_CorruptedChaos_AuraOfPain)))
		{
			Log.Info(LogText.PainZoneStarted());
		}
	}

	public bool OwnsZone(RoomModifierBase modifier)
	{
		return _zone.Owns(modifier);
	}

	public void OnEnemyDied(EnemyDeath death)
	{
		if (death.Tier != EnemyTier.Pain)
		{
			return;
		}

		if (_enemies.CountAlive(EnemyTier.Pain) > 0 || _hasPending(EnemyTier.Pain))
		{
			return;
		}

		_zone.Remove();
		PainRewards.Grant(death.Position);
		ChatNotices.Broadcast(Messages.PainFell());
		GroupFell?.Invoke();
	}

	public void Tick(double now, PainSettings settings)
	{
		_aura.Tick(now, _enemies.AliveOf(EnemyTier.Pain), settings);
	}

	public void Reset()
	{
		_zone.Remove();
		_aura.Reset();
	}
}
