using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public static class GraphemeBreakClassTrieGenerator
    {
        public static void Execute()
        {
            if (!Directory.Exists("Generated"))
            {
                Directory.CreateDirectory("Generated");
            }

            using (var stream = File.Create("Generated\\GraphemeBreak.trie"))
            {
                var trie = GenerateBreakTypeTrie();

                trie.Save(stream);
            }
        }

        private static UnicodeTrie GenerateBreakTypeTrie()
        {
            var trieBuilder = new UnicodeTrieBuilder();

            var graphemeBreakData = ReadBreakData(Path.Combine(UnicodeDataGenerator.Ucd, "auxiliary/GraphemeBreakProperty.txt"));

            var emojiBreakData = ReadBreakData(Path.Combine(UnicodeDataGenerator.Ucd, "emoji/emoji-data.txt"));

            foreach (var breakData in new [] { graphemeBreakData, emojiBreakData })
            {
                foreach (var (start, end, graphemeBreakType) in breakData)
                {
                    if (!Enum.TryParse<GraphemeBreakClass>(graphemeBreakType, out var value))
                    {
                        continue;
                    }

                    if (start == end)
                    {
                        trieBuilder.Set(start, (uint)value);
                    }
                    else
                    {
                        trieBuilder.SetRange(start, end, (uint)value);
                    }
                }
            }

            return trieBuilder.Freeze();
        }

        public static List<(int, int, string)> ReadBreakData(string file)
        {
            var data = new List<(int, int, string)>();

            var rx = new Regex(@"([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(\w+)\s*#.*", RegexOptions.Compiled);

            using (var client = new HttpClient())
            {
                using (var result = client.GetAsync(file).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        return data;
                    }

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            var match = rx.Match(line);

                            if (!match.Success)
                            {
                                continue;
                            }

                            var start = Convert.ToInt32(match.Groups[1].Value, 16);

                            var end = start;

                            if (!string.IsNullOrEmpty(match.Groups[2].Value))
                            {
                                end = Convert.ToInt32(match.Groups[2].Value, 16);
                            }

                            var breakType = match.Groups[3].Value;

                            data.Add((start, end, breakType));
                        }
                    }
                }
            }

            return data;
        }
    }
}
