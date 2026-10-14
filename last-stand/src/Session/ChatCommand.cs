using System.Globalization;
using System.Text;
using LastStand.Text;

namespace LastStand.Session;

internal static class ChatCommand
{
	public static string Parse(string content)
	{
		if (content == null)
		{
			return null;
		}

		StringBuilder visible = new StringBuilder(content.Length);
		foreach (char c in content)
		{
			if (!char.IsControl(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
			{
				visible.Append(c);
			}
		}

		string text = visible.ToString().Trim();
		return text.StartsWith(CommandText.ChatCommandPrefix) ? text.ToLowerInvariant() : null;
	}
}
