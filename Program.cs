using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

internal static class Program
{
    // Official NASA rankings
    private static readonly Dictionary<string, int> OfficialRankings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Box of matches"] = 15,
        ["Food concentrate"] = 4,
        ["50 feet of nylon rope"] = 6,
        ["Parachute silk"] = 8,
        ["Portable heating unit"] = 13,
        ["Two .45 caliber pistols"] = 11,
        ["One case of dehydrated milk"] = 12,
        ["Two 100 lb. tanks of oxygen"] = 1,
        ["Stellar map"] = 3,
        ["Self-inflating life raft"] = 9,
        ["Magnetic compass"] = 14,
        ["20 liters of water"] = 2,
        ["Signal flares"] = 10,
        ["First aid kit, including injection needle"] = 7,
        ["Solar-powered FM receiver-transmitter"] = 5
    };

    private const string SourcePath = @"C:\Users\Ben\Desktop\CCC\C#\NASA Project\NASA story and items.txt";

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string intro;
        var items = LoadIntroAndItems(SourcePath, out intro);

        if (!string.IsNullOrWhiteSpace(intro))
        {
            Console.WriteLine(intro.Trim());
            Console.WriteLine();
        }

        Console.WriteLine("Items to rank (1 = most important, 15 = least important):");
        for (int i = 0; i < items.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {items[i]}");
        }

        Console.WriteLine();
        Console.WriteLine("Enter a unique rank (1-15) for each item. Ranks must not repeat.");
        Console.WriteLine();

        var userRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var usedRanks = new HashSet<int>();

        foreach (var item in items)
        {
            int rank = PromptForUniqueRank(item, usedRanks, 1, items.Count);
            userRanks[item] = rank;
            usedRanks.Add(rank);
        }

        Console.WriteLine();
        Console.WriteLine("Results:");
        Console.WriteLine("Item".PadRight(48) + "You".PadRight(6) + "NASA".PadRight(6) + "Diff");
        Console.WriteLine(new string('-', 72));

        int totalScore = 0;
        foreach (var item in items)
        {
            int userRank = userRanks[item];
            int officialRank = GetOfficialRankForItem(item);
            int diff = Math.Abs(userRank - officialRank);
            totalScore += diff;
            Console.WriteLine(item.PadRight(48) + userRank.ToString().PadRight(6) + officialRank.ToString().PadRight(6) + diff);
        }

        Console.WriteLine();
        Console.WriteLine($"Total score: {totalScore}");
        Console.WriteLine(MapScoreToAssessment(totalScore));
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(intercept: true);
    }

    private static List<string> LoadIntroAndItems(string path, out string intro)
    {
        intro = string.Empty;
        var items = new List<string>();

        try
        {
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                // Treat first non-empty block as intro until a blank line; remaining non-empty lines are items.
                int idx = 0;
                var introLines = new List<string>();
                while (idx < lines.Length && !string.IsNullOrWhiteSpace(lines[idx]))
                {
                    introLines.Add(lines[idx]);
                    idx++;
                }

                while (idx < lines.Length && string.IsNullOrWhiteSpace(lines[idx])) idx++;

                var itemLines = new List<string>();
                for (; idx < lines.Length; idx++)
                {
                    var line = lines[idx].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // remove common leading numbering like "1. " or "- "
                    line = Regex.Replace(line, @"^\s*(\d+\.)\s*", "");
                    line = Regex.Replace(line, @"^\s*-\s*", "");
                    itemLines.Add(line);
                }

                if (introLines.Count > 0) intro = string.Join(Environment.NewLine, introLines);
                if (itemLines.Count > 0) items = itemLines;
            }
        }
        catch
        {
            // ignore parse errors and fall back to defaults
        }

        if (items.Count != OfficialRankings.Count)
        {
            // fallback to official keys (preserves official ordering by the dictionary initializer order)
            items = OfficialRankings.Keys.ToList();
            if (string.IsNullOrWhiteSpace(intro))
            {
                intro = "Scenario: You are stranded and must prioritize the following items for survival. Rank them from 1 (most important) to 15 (least important).";
            }
        }

        return items;
    }

    private static int PromptForUniqueRank(string item, HashSet<int> used, int min, int max)
    {
        while (true)
        {
            Console.Write($"Rank for \"{item}\": ");
            var input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out int val))
            {
                Console.WriteLine("Invalid input — enter an integer.");
                continue;
            }

            if (val < min || val > max)
            {
                Console.WriteLine($"Rank must be between {min} and {max}.");
                continue;
            }

            if (used.Contains(val))
            {
                Console.WriteLine("That rank has already been used. Choose a different rank.");
                continue;
            }

            return val;
        }
    }

    private static int GetOfficialRankForItem(string item)
    {
        if (OfficialRankings.TryGetValue(item, out int r)) return r;
        // fuzzy fallback: pick the official entry with most tokens in common
        var tokens = item.Split(new[] { ' ', ',', '.', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.ToLowerInvariant()).Where(t => t.Length > 2).ToArray();

        string bestKey = null;
        int bestScore = -1;
        foreach (var kvp in OfficialRankings)
        {
            int score = 0;
            var keyLower = kvp.Key.ToLowerInvariant();
            foreach (var t in tokens) if (keyLower.Contains(t)) score++;
            if (score > bestScore)
            {
                bestScore = score;
                bestKey = kvp.Key;
            }
        }

        return bestKey != null ? OfficialRankings[bestKey] : 8; // middle default
    }

    private static string MapScoreToAssessment(int score)
    {
        if (score >= 0 && score <= 25) return "Assessment: 0 - 25: excellent";
        if (score <= 32) return "Assessment: 26 - 32: good";
        if (score <= 45) return "Assessment: 33 - 45: average";
        if (score <= 55) return "Assessment: 46 - 55: fair";
        if (score <= 70) return "Assessment: 56 - 70: poor";
        return "Assessment: 71 - 112: very poor";
    }
}