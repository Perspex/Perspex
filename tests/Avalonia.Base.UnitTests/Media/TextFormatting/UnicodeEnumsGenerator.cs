using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    internal static class UnicodeEnumsGenerator
    {
        public static List<DataEntry> CreateScriptEnum()
        {
            var entries = new List<DataEntry>
            {
                new DataEntry("Unknown", "Zzzz", string.Empty),
                new DataEntry("Common", "Zyyy", string.Empty),
                new DataEntry("Inherited", "Zinh", string.Empty)
            };

            ParseDataEntries("# Script (sc)", entries);

            using (var stream = File.Create("Generated\\Script.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum Script");
                writer.WriteLine("    {");

                foreach (var entry in entries)
                {
                    writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                                     (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return entries;
        }

        public static List<DataEntry> CreateGeneralCategoryEnum()
        {
            var entries = new List<DataEntry> { new DataEntry("Other", "C", " Cc | Cf | Cn | Co | Cs") };

            ParseDataEntries("# General_Category (gc)", entries);

            using (var stream = File.Create("Generated\\GeneralCategory.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum GeneralCategory");
                writer.WriteLine("    {");

                foreach (var entry in entries)
                {
                    writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                                     (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return entries;
        }

        public static List<DataEntry> CreateGraphemeBreakTypeEnum()
        {
            var entries = new List<DataEntry> { new DataEntry("Other", "XX", string.Empty) };

            ParseDataEntries("# Grapheme_Cluster_Break (GCB)", entries);

            using (var stream = File.Create("Generated\\GraphemeBreakClass.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum GraphemeBreakClass");
                writer.WriteLine("    {");

                foreach (var entry in entries)
                {
                    writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                                     (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
                }

                writer.WriteLine("        ExtendedPictographic");

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return entries;
        }

        private static List<string> GenerateBreakPairTable()
        {
            var rows = new List<string[]>();

            using (var stream =
                typeof(UnicodeEnumsGenerator).Assembly.GetManifestResourceStream(
                    "Avalonia.Visuals.UnitTests.Media.TextFormatting.BreakPairTable.txt"))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    var columns = line.Split('\t');

                    rows.Add(columns);
                }
            }

            using (var stream = File.Create("Generated\\BreakPairTable.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    internal static class BreakPairTable");
                writer.WriteLine("    {");

                writer.WriteLine("        private static readonly byte[][] s_breakPairTable = ");
                writer.WriteLine("            {");

                for (var i = 1; i < rows.Count; i++)
                {
                    writer.Write("             new byte[] {");

                    writer.Write($"{GetBreakPairType(rows[i][1])}");

                    for (var index = 2; index < rows[i].Length; index++)
                    {
                        var column = rows[i][index];

                        writer.Write($",{GetBreakPairType(column)}");
                    }

                    writer.Write("},");
                    writer.Write(Environment.NewLine);
                }
                writer.WriteLine("        };");

                writer.WriteLine();

                writer.WriteLine("        public static PairBreakType Map(LineBreakClass first, LineBreakClass second)");
                writer.WriteLine("        {");
                writer.WriteLine("            return (PairBreakType)s_breakPairTable[(int)first][(int)second];");
                writer.WriteLine("        }");

                writer.WriteLine("    }");

                writer.WriteLine();

                writer.WriteLine("    internal enum PairBreakType : byte");
                writer.WriteLine("    {");
                writer.WriteLine("        DI = 0, // Direct break opportunity");
                writer.WriteLine("        IN = 1, // Indirect break opportunity");
                writer.WriteLine("        CI = 2, // Indirect break opportunity for combining marks");
                writer.WriteLine("        CP = 3, // Prohibited break for combining marks");
                writer.WriteLine("        PR = 4 // Prohibited break");
                writer.WriteLine("    }");


                writer.WriteLine("}");
            }

            return rows[0].Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public const byte DI_BRK = 0; // Direct break opportunity
        public const byte IN_BRK = 1; // Indirect break opportunity
        public const byte CI_BRK = 2; // Indirect break opportunity for combining marks
        public const byte CP_BRK = 3; // Prohibited break for combining marks
        public const byte PR_BRK = 4; // Prohibited break

        private static byte GetBreakPairType(string type)
        {
            switch (type)
            {
                case "_":
                    return DI_BRK;
                case "%":
                    return IN_BRK;
                case "#":
                    return CI_BRK;
                case "@":
                    return CP_BRK;
                case "^":
                    return PR_BRK;
                default:
                    return byte.MaxValue;
            }
        }

        public static List<DataEntry> CreateLineBreakClassEnum()
        {
            var usedLineBreakClasses = GenerateBreakPairTable();

            var entries = new List<DataEntry> { new DataEntry("Unknown", "XX", string.Empty) };

            ParseDataEntries("# Line_Break (lb)", entries);

            var orderedLineBreakEntries = new Dictionary<string, DataEntry>();

            foreach (var tag in usedLineBreakClasses)
            {
                var entry = entries.Single(x => x.Tag == tag);

                orderedLineBreakEntries.Add(tag, entry);
            }

            foreach (var entry in entries)
            {
                if (orderedLineBreakEntries.ContainsKey(entry.Tag))
                {
                    continue;
                }

                orderedLineBreakEntries.Add(entry.Tag, entry);
            }

            using (var stream = File.Create("Generated\\LineBreakClass.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum LineBreakClass");
                writer.WriteLine("    {");

                foreach (var entry in orderedLineBreakEntries.Values)
                {
                    writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                                     (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return orderedLineBreakEntries.Values.ToList();
        }

        public static List<DataEntry> CreateBiDiClassEnum()
        {
            var entries = new List<DataEntry> { new DataEntry("Left_To_Right", "L", string.Empty) };

            ParseDataEntries("# Bidi_Class (bc)", entries);

            using (var stream = File.Create("Generated\\BiDiClass.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    public enum BiDiClass");
                writer.WriteLine("    {");

                foreach (var entry in entries)
                {
                    writer.WriteLine("        " + entry.Name.Replace("_", "") + ", //" + entry.Tag +
                                     (string.IsNullOrEmpty(entry.Comment) ? string.Empty : "#" + entry.Comment));
                }

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }

            return entries;
        }

        public static void CreatePropertyValueAliasHelper(List<DataEntry> scriptEntries, IEnumerable<DataEntry> generalCategoryEntries,
            IEnumerable<DataEntry> biDiClassEntries, IEnumerable<DataEntry> lineBreakClassEntries)
        {
            using (var stream = File.Create("Generated\\PropertyValueAliasHelper.cs"))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine();

                writer.WriteLine("namespace Avalonia.Media.TextFormatting.Unicode");
                writer.WriteLine("{");
                writer.WriteLine("    internal static class PropertyValueAliasHelper");
                writer.WriteLine("    {");

                WritePropertyValueAliasGetTag(writer, scriptEntries, "Script", "Zzzz");

                WritePropertyValueAlias(writer, scriptEntries, "Script", "Unknown");

                WritePropertyValueAlias(writer, generalCategoryEntries, "GeneralCategory", "Other");

                WritePropertyValueAlias(writer, biDiClassEntries, "BiDiClass", "LeftToRight");

                WritePropertyValueAlias(writer, lineBreakClassEntries, "LineBreakClass", "Unknown");

                writer.WriteLine("    }");
                writer.WriteLine("}");
            }
        }

        public static void ParseDataEntries(string property, List<DataEntry> entries)
        {
            using (var client = new HttpClient())
            {
                var url = Path.Combine(UnicodeDataGenerator.Ucd, "PropertyValueAliases.txt");

                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                    {
                        return;
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

                            if (line != property)
                            {
                                continue;
                            }

                            reader.ReadLine();

                            break;
                        }

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                            {
                                break;
                            }

                            var elements = line.Split(';');

                            var tag = elements[1].Trim();

                            elements = elements[2].Split('#');

                            var name = elements[0].Trim();

                            if (entries.Any(x => x.Name == name))
                            {
                                continue;
                            }

                            var comment = string.Empty;

                            if (elements.Length > 1)
                            {
                                comment = elements[1];
                            }

                            var entry = new DataEntry(name, tag, comment);

                            entries.Add(entry);
                        }
                    }
                }
            }
        }

        private static void WritePropertyValueAliasGetTag(TextWriter writer, IEnumerable<DataEntry> entries,
            string typeName, string defaultValue)
        {
            writer.WriteLine(
                $"        private static readonly Dictionary<{typeName}, string> s_{typeName.ToLower()}ToTag = ");
            writer.WriteLine($"            new Dictionary<{typeName}, string>{{");

            foreach (var entry in entries)
            {
                writer.WriteLine($"                {{ {typeName}.{entry.Name.Replace("_", "")}, \"{entry.Tag}\"}},");
            }

            writer.WriteLine("        };");

            writer.WriteLine();

            writer.WriteLine($"        public static string GetTag({typeName} {typeName.ToLower()})");
            writer.WriteLine("        {");
            writer.WriteLine($"            if(!s_{typeName.ToLower()}ToTag.ContainsKey({typeName.ToLower()}))");
            writer.WriteLine("            {");
            writer.WriteLine($"                return \"{defaultValue}\";");
            writer.WriteLine("            }");
            writer.WriteLine($"            return s_{typeName.ToLower()}ToTag[{typeName.ToLower()}];");
            writer.WriteLine("        }");

            writer.WriteLine();
        }

        private static void WritePropertyValueAlias(TextWriter writer, IEnumerable<DataEntry> entries, string typeName,
            string defaultValue)
        {
            writer.WriteLine($"        private static readonly Dictionary<string, {typeName}> s_tagTo{typeName} = ");
            writer.WriteLine($"            new Dictionary<string,{typeName}>{{");

            foreach (var entry in entries)
            {
                writer.WriteLine($"                {{ \"{entry.Tag}\", {typeName}.{entry.Name.Replace("_", "")}}},");
            }

            writer.WriteLine("        };");

            writer.WriteLine();

            writer.WriteLine($"        public static {typeName} Get{typeName}(string tag)");
            writer.WriteLine("        {");
            writer.WriteLine($"            if(!s_tagTo{typeName}.ContainsKey(tag))");
            writer.WriteLine("            {");
            writer.WriteLine($"                return {typeName}.{defaultValue};");
            writer.WriteLine("            }");
            writer.WriteLine($"            return s_tagTo{typeName}[tag];");
            writer.WriteLine("        }");

            writer.WriteLine();
        }
    }

    public readonly struct DataEntry
    {
        public DataEntry(string name, string tag, string comment)
        {
            Name = name;
            Tag = tag;
            Comment = comment;
        }

        public string Name { get; }
        public string Tag { get; }
        public string Comment { get; }
    }
}
