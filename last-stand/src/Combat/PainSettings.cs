namespace LastStand.Combat;

internal sealed class PainSettings
{
	public float DamageRatioPerSecond { get; }

	public float HealthBonusPercent { get; }

	public float DamageBonusPercent { get; }

	public PainSettings(float damageRatioPerSecond, float healthBonusPercent, float damageBonusPercent)
	{
		DamageRatioPerSecond = damageRatioPerSecond;
		HealthBonusPercent = healthBonusPercent;
		DamageBonusPercent = damageBonusPercent;
	}
}
