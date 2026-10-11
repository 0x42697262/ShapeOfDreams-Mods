using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LastStand.Game;

internal sealed class PlayerKeys
{
	private const char ProfileDash = '-';

	private readonly Dictionary<DewPlayer, string> _keys = new Dictionary<DewPlayer, string>();

	private readonly HashSet<string> _present = new HashSet<string>();

	public string Of(DewPlayer player)
	{
		if (player == null)
		{
			return ModIdentity.NoPlayerKey;
		}

		if (_keys.TryGetValue(player, out string key))
		{
			return key;
		}

		if (DewPlayer.gamePlayers == null || !DewPlayer.gamePlayers.Contains(player))
		{
			return ModIdentity.NoPlayerKey;
		}

		key = KeyFor(player);
		_keys[player] = key;
		return key;
	}

	public HashSet<string> Present()
	{
		_present.Clear();
		_present.Add(ModIdentity.NoPlayerKey);
		if (DewPlayer.gamePlayers == null)
		{
			return _present;
		}

		foreach (DewPlayer player in DewPlayer.gamePlayers)
		{
			if (player != null)
			{
				_present.Add(Of(player));
			}
		}

		return _present;
	}

	public void Clear()
	{
		_keys.Clear();
		_present.Clear();
	}

	private static string KeyFor(DewPlayer player)
	{
		ulong steam = player.steamId.m_SteamID;
		if (steam != 0)
		{
			return ModIdentity.SteamPlayerKeyPrefix + steam.ToString(CultureInfo.InvariantCulture);
		}

		string profile = Clean(player.guid);
		return profile.Length > 0
			? ModIdentity.ProfilePlayerKeyPrefix + profile
			: ModIdentity.NetPlayerKeyPrefix + player.netId.ToString(CultureInfo.InvariantCulture);
	}

	private static string Clean(string text)
	{
		StringBuilder clean = new StringBuilder();
		foreach (char c in text ?? string.Empty)
		{
			if (char.IsLetterOrDigit(c) || c == ProfileDash)
			{
				clean.Append(c);
			}
		}

		return clean.ToString();
	}
}
