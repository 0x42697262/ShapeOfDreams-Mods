using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LastStand.Config;

namespace LastStand.Waves;

internal sealed class PaceLedger
{
	private const char ListSeparator = '/';

	private const char RecordSeparator = ';';

	private const char PartSeparator = ':';

	private const char EntrySeparator = ',';

	private const char ValueSeparator = '=';

	private const int ListCount = 2;

	private const int PartCount = 2;

	private readonly List<FastClearRecord> _fastClears = new List<FastClearRecord>();

	private readonly List<HeroicFastKillRecord> _heroicKills = new List<HeroicFastKillRecord>();

	public int FastClearCount => _fastClears.Count;

	public int HeroicKillCount => _heroicKills.Count;

	public void Add(FastClearRecord record)
	{
		_fastClears.Add(record);
	}

	public void Add(HeroicFastKillRecord record)
	{
		_heroicKills.Add(record);
	}

	public int FastClearStacks(ICollection<string> present)
	{
		int stacks = 0;
		foreach (FastClearRecord record in _fastClears)
		{
			if (record.HoldsFor(present))
			{
				stacks++;
			}
		}

		return stacks;
	}

	public double HeroicKillMultiplier(HeroicFastKillRule rule, ICollection<string> present)
	{
		double multiplier = HeroicFastKillRule.NoChange;
		foreach (HeroicFastKillRecord record in _heroicKills)
		{
			double share = record.ShareOf(present);
			if (share > 0)
			{
				multiplier *= rule.StepFor(record.ProjectedSecondsPerBoss / share);
			}
		}

		return multiplier;
	}

	public string Serialize()
	{
		StringBuilder text = new StringBuilder();
		for (int i = 0; i < _fastClears.Count; i++)
		{
			AppendRecord(text, i, _fastClears[i].Needed.ToString(CultureInfo.InvariantCulture),
				_fastClears[i].InstantKillsByPlayer, count => count.ToString(CultureInfo.InvariantCulture));
		}

		text.Append(ListSeparator);
		for (int i = 0; i < _heroicKills.Count; i++)
		{
			AppendRecord(text, i, _heroicKills[i].ProjectedSecondsPerBoss.ToString(Tuning.RoundTripNumberFormat, CultureInfo.InvariantCulture),
				_heroicKills[i].BurstDamageByPlayer, damage => damage.ToString(Tuning.RoundTripNumberFormat, CultureInfo.InvariantCulture));
		}

		return text.ToString();
	}

	public static PaceLedger Parse(string text)
	{
		PaceLedger ledger = new PaceLedger();
		string[] lists = (text ?? string.Empty).Split(ListSeparator);
		if (lists.Length != ListCount)
		{
			return ledger;
		}

		foreach (string record in Records(lists[0]))
		{
			string[] parts = record.Split(PartSeparator);
			if (parts.Length == PartCount &&
			    int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int needed))
			{
				ledger.Add(new FastClearRecord(needed, Entries<int>(parts[1], TryParseInt)));
			}
		}

		foreach (string record in Records(lists[1]))
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

	private static bool TryParseInt(string text, out int value)
	{
		return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}

	private static bool TryParseDouble(string text, out double value)
	{
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}
}
