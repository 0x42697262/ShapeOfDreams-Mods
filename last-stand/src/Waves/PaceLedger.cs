using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LastStand.Config;

namespace LastStand.Waves;

internal sealed class PaceLedger
{
	private const char RecordSeparator = ';';

	private const char PartSeparator = ':';

	private const char EntrySeparator = ',';

	private const char ValueSeparator = '=';

	private const int PartCount = 2;

	private readonly List<HeroicFastKillRecord> _heroicKills = new List<HeroicFastKillRecord>();

	public int HeroicKillCount => _heroicKills.Count;

	public void Add(HeroicFastKillRecord record)
	{
		_heroicKills.Add(record);
	}

	public double HeroicKillMultiplier(HeroicFastKillRule rule, ICollection<string> present)
	{
		double multiplier = HeroicFastKillRule.NoChange;
		foreach (HeroicFastKillRecord record in _heroicKills)
		{
			double share = record.ShareOf(present);
			if (share > 0)
			{
				multiplier *= rule.StepFor(record.ProjectedSeconds / share);
			}
		}

		return multiplier;
	}

	public string Serialize()
	{
		StringBuilder text = new StringBuilder();
		for (int i = 0; i < _heroicKills.Count; i++)
		{
			AppendRecord(text, i, _heroicKills[i].ProjectedSeconds.ToString(Tuning.RoundTripNumberFormat, CultureInfo.InvariantCulture),
				_heroicKills[i].BurstDamageByPlayer, damage => damage.ToString(Tuning.RoundTripNumberFormat, CultureInfo.InvariantCulture));
		}

		return text.ToString();
	}

	public static PaceLedger Parse(string text)
	{
		PaceLedger ledger = new PaceLedger();
		foreach (string record in Records(text ?? string.Empty))
		{
			string[] parts = record.Split(PartSeparator);
			if (parts.Length == PartCount &&
			    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double projected))
			{
				ledger.Add(new HeroicFastKillRecord(projected, Entries<double>(parts[1], TryParseDouble)));
			}
		}

		return ledger;
	}

	private delegate bool ValueParser<T>(string text, out T value);

	private static void AppendRecord<T>(StringBuilder text, int index, string head, Dictionary<string, T> entries,
		Func<T, string> format)
	{
		if (index > 0)
		{
			text.Append(RecordSeparator);
		}

		text.Append(head).Append(PartSeparator);
		bool first = true;
		foreach (KeyValuePair<string, T> entry in entries)
		{
			if (!first)
			{
				text.Append(EntrySeparator);
			}

			text.Append(entry.Key).Append(ValueSeparator).Append(format(entry.Value));
			first = false;
		}
	}

	private static IEnumerable<string> Records(string list)
	{
		foreach (string record in list.Split(RecordSeparator))
		{
			if (record.Length > 0)
			{
				yield return record;
			}
		}
	}

	private static Dictionary<string, T> Entries<T>(string text, ValueParser<T> parse)
	{
		Dictionary<string, T> entries = new Dictionary<string, T>();
		foreach (string entry in text.Split(EntrySeparator))
		{
			string[] pair = entry.Split(ValueSeparator);
			if (pair.Length == PartCount && pair[0].Length > 0 && parse(pair[1], out T value))
			{
				entries[pair[0]] = value;
			}
		}

		return entries;
	}

	private static bool TryParseDouble(string text, out double value)
	{
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}
}
