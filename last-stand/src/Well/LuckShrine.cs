using LastStand.Text;
using UnityEngine;

namespace LastStand.Well;

internal sealed class LuckShrine
{
	private readonly StandingShrine<Shrine_Luck> _standing =
		new StandingShrine<Shrine_Luck>(LogText.ReminiscenceOfLuckName, ShrineUses.Vanilla, float.PositiveInfinity);

	public void Place(Vector3 position)
	{
		_standing.Place(position);
	}

	public void OnWaveCleared(int wave)
	{
		Shrine_Luck shrine = _standing.Current;
		if (shrine.IsNullOrInactive() || DewPlayer.gamePlayers == null)
		{
			return;
		}

		shrine.ResetUseCounts();
		shrine.currentPrayCount.Clear();
		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null)
			{
				shrine.PopulatePlayerChoices(player);
			}
		}

		Log.Info(LogText.LuckChoicesRenewed(wave));
	}

	public void Forget()
	{
		_standing.Forget();
	}
}
