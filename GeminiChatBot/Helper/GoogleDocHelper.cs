using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace GeminiChatBot.Helper
{
    public static class GoogleDocHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private static readonly ConcurrentDictionary<string, (string Content, DateTime Expiry)> _cache
            = new ConcurrentDictionary<string, (string, DateTime)>();

        public static async Task<string> GetGoogleDocContentAsync(string googleLink, string kotaName = null)
        {
            if (_cache.TryGetValue(googleLink, out var cacheEntry))
            {
                if (DateTime.UtcNow < cacheEntry.Expiry)
                    return cacheEntry.Content;
            }

            string url = googleLink;
            string content = null;

            if (url.Contains("docs.google.com/document"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
                if (!match.Success)
                    throw new ArgumentException("Invalid Google Docs link");

                string docId = match.Groups[1].Value;
                url = $"https://docs.google.com/document/d/{docId}/export?format=txt";

                content = await httpClient.GetStringAsync(url);
            }
            else if (url.Contains("docs.google.com/spreadsheets"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
                if (!match.Success)
                    throw new ArgumentException("Invalid Google Sheets link");

                string sheetId = match.Groups[1].Value;
                url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";

                var csvContent = await httpClient.GetStringAsync(url);

                if (string.IsNullOrEmpty(kotaName))
                {
                    content = csvContent;
                }
                else
                {
                    int month = DateTime.Now.Month;

                    var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length <= 1)
                    {
                        content = csvContent;
                    }
                    else
                    {
                        var header = lines[0].Split(',');
                        var resultLines = new List<string> { string.Join(',', header) };

                        int kotaIndex = Array.FindIndex(header, h => h.Equals("kota", StringComparison.OrdinalIgnoreCase));
                        int bulanIndex = Array.FindIndex(header, h => h.Equals("bulan", StringComparison.OrdinalIgnoreCase));

                        foreach (var line in lines.Skip(1))
                        {
                            var cols = line.Split(',');
                            if (cols.Length != header.Length) continue;

                            bool matchKota = string.IsNullOrEmpty(kotaName) ||
                                             cols[kotaIndex].Contains(kotaName, StringComparison.OrdinalIgnoreCase);

                            bool matchBulan = cols[bulanIndex].Trim() == DateTime.Now.Month.ToString();

                            if (matchKota && matchBulan)
                            {
                                resultLines.Add(line);
                            }
                        }

                        content = string.Join('\n', resultLines);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Unsupported Google link. Must be Docs or Sheets.");
            }

            _cache[googleLink] = (content, DateTime.UtcNow.AddHours(24));

            return content;
        }
    }

}
