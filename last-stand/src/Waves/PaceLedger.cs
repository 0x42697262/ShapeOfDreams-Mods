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

	private const int RecordPartCount = 3;

	private const int PairPartCount = 2;

	private readonly List<HeroicFastKillRecord> _heroicKills = new List<HeroicFastKillRecord>();

	public int HeroicKillCount => _heroicKills.Count;

	public void Add(HeroicFastKillRecord record)
	{
		_heroicKills.Add(record);
	}

	public double RaisedWorlds(HeroicFastKillRule rule, ICollection<string> present,
		HeroicFastKillRule.WorldSolver solver)
	{
		if (!rule.IsOn)
		{
			return 0;
		}

		double worlds = 0;
		foreach (HeroicFastKillRecord record in _heroicKills)
		{
			worlds += rule.RaiseFor(record, record.ShareOf(present), solver);
		}

		return worlds;
	}

	public string Serialize()
	{
		StringBuilder text = new StringBuilder();
		for (int i = 0; i < _heroicKills.Count; i++)
		{
			HeroicFastKillRecord record = _heroicKills[i];
			AppendRecord(text, i, Number(record.ProjectedSeconds) + PartSeparator + Number(record.FromExtraWorlds),
				record.BurstDamageByPlayer, Number);
		}

		return text.ToString();
	}

	public static PaceLedger Parse(string text)
	{
		PaceLedger ledger = new PaceLedger();
		foreach (string record in Records(text ?? string.Empty))
		{
			string[] parts = record.Split(PartSeparator);
			if (parts.Length == RecordPartCount && TryParseDouble(parts[0], out double projected) &&
			    TryParseDouble(parts[1], out double fromExtraWorlds))
			{
				ledger.Add(new HeroicFastKillRecord(projected, fromExtraWorlds, Entries<double>(parts[2], TryParseDouble)));
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
			if (pair.Length == PairPartCount && pair[0].Length > 0 && parse(pair[1], out T value))
			{
				entries[pair[0]] = value;
			}
		}

		return entries;
	}

	private static string Number(double value)
	{
		return value.ToString(Tuning.RoundTripNumberFormat, CultureInfo.InvariantCulture);
	}

	private static bool TryParseDouble(string text, out double value)
	{
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}
}
