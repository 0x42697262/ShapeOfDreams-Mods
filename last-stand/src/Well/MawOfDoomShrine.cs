using LastStand.Config;
using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class MawOfDoomShrine
{
	private readonly StandingShrine<Shrine_MawOfDoom> _standing =
		new StandingShrine<Shrine_MawOfDoom>(LogText.MawOfDoomName, ShrineUses.Vanilla, float.PositiveInfinity);

	public void Place(Vector3 position)
	{
		_standing.Place(position);
		PinUses();
	}

	public void OnUsed(Shrine shrine)
	{
		if (_standing.Owns(shrine))
		{
			PinUses();
		}
	}

	public void PinUses()
	{
		UseCountPin.Pin(_standing.Current, Tuning.MawOfDoomPinnedUses);
	}

	public void Forget()
	{
		_standing.Forget();
	}
}
