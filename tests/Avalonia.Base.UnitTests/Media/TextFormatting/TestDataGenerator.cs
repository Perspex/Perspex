using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public abstract class TestDataGenerator : IEnumerable<object[]>
    {
        private readonly string _fileName;
        private readonly List<object[]> _testData;

        protected TestDataGenerator(string fileName)
        {
            _fileName = fileName;
            _testData = ReadTestData();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<object[]> ReadTestData()
        {
            var testData = new List<object[]>();

            using (var client = new HttpClient())
            {
                var url = Path.Combine(UnicodeDataGenerator.Ucd, _fileName);

                using (var result = client.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (!result.IsSuccessStatusCode)
                        return testData;

                    using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();

                            if (line == null)
                            {
                                break;
                            }

                            if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            var elements = line.Split('#');

                            elements = elements[0].Replace("÷\t", "÷").Trim('÷').Split('÷');

                            var chars = elements[0].Replace(" × ", " ").Split(' ');

                            var codepoints = chars.Where(x => x != "" && x != "×")
                                .Select(x => Convert.ToInt32(x, 16)).ToArray();

                            var text = string.Join(null, codepoints.Select(char.ConvertFromUtf32));

                            var length = codepoints.Select(x => x > ushort.MaxValue ? 2 : 1).Sum();

                            var data = new object[] { text, length };

                            testData.Add(data);
                        }
                    }
                }
            }

            return testData;
        }
    }
}
