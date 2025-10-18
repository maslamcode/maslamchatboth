using Chatbot.Service.Model.Chatbot;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Chatbot.Service.Services.Chatbot
{
    public class ChatbotService : IChatbotService
    {
        private readonly string _connectionString;
        private readonly string _googleApiKey;
        private readonly string _geminiVersion;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public ChatbotService(IConfiguration config)
        {
            _config = config;
            _googleApiKey = _config["Google:GeminiApiKey"];
            _geminiVersion = _config["Google:GeminiVersi"];
            _connectionString = _config.GetConnectionString("PostgreSqlConnection") ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        private async Task<IEnumerable<string>> GetGreetingResponsesAsync()
        {
            using var conn = GetConnection();
            var sql = "SELECT tag_message FROM chatbot.chatbot_respone WHERE type = 1";
            return await conn.QueryAsync<string>(sql);
        }

        private async Task<IEnumerable<string>> GetSayHaiResponsesAsync()
        {
            using var conn = GetConnection();
            var sql = "SELECT tag_message FROM chatbot.chatbot_respone WHERE type = 2 ORDER BY \"order\"";
            return await conn.QueryAsync<string>(sql);
        }

        public async Task<string> HandlePromptGreetingsAsync(string prompt)
        {
            var greetings = new List<string> { "hai", "halo", "salam", "asslamualaikum", "selamat pagi", "selamat siang", "selamat malam", "apa kabar" };
            var who = new List<string> { "siapa", "anda" };

            // greeting check
            var greetResp = await GetGreetingResponsesAsync();
            var sayHaiResp = await GetSayHaiResponsesAsync();

            if (greetings.Any(g => prompt.Contains(g, StringComparison.OrdinalIgnoreCase)))
            {
                var resp = "wa'alaykumsalam wr wb\n" + string.Join("\n", greetResp);
                return resp;
            }

            if (prompt.Contains("siapa") && (prompt.Contains("anda") || prompt.Contains("kamu")))
            {
                return string.Join("\n", sayHaiResp);
            }

            if (prompt.Length < 5)
            {
                return "Pertanyaan terlalu singkat, silakan ajukan pertanyaan yang lebih jelas.";
            }

            return string.Empty;
        }

        public async Task<IEnumerable<string>> GetMatchedDataLinksAsync(string prompt)
        {
            using var conn = GetConnection();

            var sql = @"SELECT chatbot_data_id, 
                               prompt_words, data_link_online
                        FROM chatbot.chatbot_data";

            var allData = await conn.QueryAsync<ChatbotDataModel>(sql);

            var matchedLinks = new List<string>();

            if (allData == null || !allData.Any())
                return matchedLinks;

            var promptWords = Regex.Split(prompt, @"\W+")
                                  .Where(w => !string.IsNullOrWhiteSpace(w))
                                  .Select(w => w.ToLowerInvariant())
                                  .ToHashSet();

            foreach (var selection in allData)
            {
                if (string.IsNullOrWhiteSpace(selection.prompt_words) || string.IsNullOrWhiteSpace(selection.data_link_online))
                    continue;

                var keywords = selection.prompt_words
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(k => k.ToLowerInvariant());

                //if (keywords.Any(k => words.Any(w => string.Equals(w, k, StringComparison.OrdinalIgnoreCase)))) //TODO - SOON
                //if (keywords.Any(k => prompt.Contains(k, StringComparison.OrdinalIgnoreCase)))
                //{
                //    if (!matchedLinks.Contains(selection.data_link_online))
                //        matchedLinks.Add(selection.data_link_online);
                //}

                bool matched = keywords.Any(k =>
                {
                    // Escape regex characters
                    var escaped = Regex.Escape(k);

                    // If the keyword contains a space, just search the whole phrase (ignore case)
                    if (k.Contains(' ', StringComparison.Ordinal))
                    {
                        return Regex.IsMatch(prompt, escaped, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // Single word: match as whole word with \b boundaries
                        return Regex.IsMatch(prompt, $@"\b{escaped}\b", RegexOptions.IgnoreCase);
                    }
                });

                if (matched && !matchedLinks.Contains(selection.data_link_online))
                    matchedLinks.Add(selection.data_link_online);

            }

            return matchedLinks;
        }

        public async Task<IEnumerable<string>> GetMatchedDataFilesAsync(string prompt)
        {
            using var conn = GetConnection();

            var sql = @"SELECT chatbot_files_id,  
                               prompt_words, file_name
                        FROM chatbot.chatbot_files";

            var allData = await conn.QueryAsync<ChatbotFileModel>(sql);

            var matchedFileNames = new List<string>();

            if (allData == null || !allData.Any())
                return matchedFileNames;

            var promptWords = Regex.Split(prompt, @"\W+")
                                   .Where(w => !string.IsNullOrWhiteSpace(w))
                                   .Select(w => w.ToLowerInvariant())
                                   .ToHashSet();


            foreach (var selection in allData)
            {
                if (string.IsNullOrWhiteSpace(selection.prompt_words) || string.IsNullOrWhiteSpace(selection.file_name))
                    continue;

                var keywords = selection.prompt_words
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(k => k.ToLowerInvariant());


                //I have the issue here, if the prompt is "Apa itu product line", so on database have 2 records, the keywords 1: "product line, product abaga", keywords 2: "PR, Ziswaf"
                //With the currently the prompt "Apa itu product line" will match both records, because "PR" is part of "product line", I want only match the first record, PR not include

                //if (keywords.Any(k => words.Any(w => string.Equals(w, k, StringComparison.OrdinalIgnoreCase)))) //TODO - SOON
                //if (keywords.Any(k => prompt.Contains(k, StringComparison.OrdinalIgnoreCase)))
                //{
                //    if (!matchedFileNames.Contains(selection.file_name))
                //        matchedFileNames.Add(selection.file_name);
                //}

                bool matched = keywords.Any(k =>
                {
                    // Escape regex characters
                    var escaped = Regex.Escape(k);

                    // If the keyword contains a space, just search the whole phrase (ignore case)
                    if (k.Contains(' ', StringComparison.Ordinal))
                    {
                        return Regex.IsMatch(prompt, escaped, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // Single word: match as whole word with \b boundaries
                        return Regex.IsMatch(prompt, $@"\b{escaped}\b", RegexOptions.IgnoreCase);
                    }
                });

                if (matched && !matchedFileNames.Contains(selection.file_name))
                    matchedFileNames.Add(selection.file_name);


            }

            return matchedFileNames;
        }

        public async Task<string> GetResponseFromGeminiAsync(object payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload);
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiVersion}:generateContent?key={_googleApiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                Console.WriteLine(requestUrl);
                Console.WriteLine(response.StatusCode);
                Console.WriteLine("Empty response from Gemini API.");
                return "Maaf, saya tidak menerima jawaban dari server. Silakan coba lagi.";
            }

            // Handle 429 (Too Many Requests)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                string retryAfterHeader = response.Headers.TryGetValues("Retry-After", out var values) ? values.FirstOrDefault() : null;

                double? waitSeconds = null;
                if (int.TryParse(retryAfterHeader, out int retryAfterSeconds))
                {
                    waitSeconds = retryAfterSeconds;
                }
                else
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("error", out var errorEl) &&
                        errorEl.TryGetProperty("details", out var detailsEl))
                    {
                        foreach (var detail in detailsEl.EnumerateArray())
                        {
                            if (detail.TryGetProperty("@type", out var typeEl) && typeEl.GetString() == "type.googleapis.com/google.rpc.RetryInfo" && detail.TryGetProperty("retryDelay", out var retryDelayEl))
                            {
                                var retryDelayStr = retryDelayEl.GetString();
                                if (!string.IsNullOrEmpty(retryDelayStr) && retryDelayStr.EndsWith("s") &&
                                    double.TryParse(retryDelayStr.TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                                {
                                    waitSeconds = seconds;
                                }
                            }
                        }
                    }
                }

                string waitMsg = waitSeconds.HasValue
                    ? $"Mohon maaf, permintaan anda melebihi batas. Coba lagi dalam {FormatWaktu(waitSeconds.Value)}."
                    : "Mohon maaf, permintaan anda melebihi batas. Silakan coba lagi nanti.";
                return waitMsg;
            }

            // Parse successful response
            using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
            {
                if (jsonDoc.RootElement.TryGetProperty("candidates", out JsonElement candidates))
                {
                    foreach (JsonElement candidate in candidates.EnumerateArray())
                    {
                        if (candidate.TryGetProperty("content", out JsonElement content) &&
                            content.TryGetProperty("parts", out JsonElement parts))
                        {
                            foreach (JsonElement part in parts.EnumerateArray())
                            {
                                if (part.TryGetProperty("text", out JsonElement textEl))
                                {
                                    string result = textEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(result))
                                        return result;
                                }
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static string FormatWaktu(double totalSeconds)
        {
            int totalDetik = (int)Math.Ceiling(totalSeconds);
            int jam = totalDetik / 3600;
            int menit = (totalDetik % 3600) / 60;
            int detik = totalDetik % 60;

            if (jam > 0)
                return $"{jam} jam {menit} menit {detik} detik";
            if (menit > 0)
                return $"{menit} menit {detik} detik";
            return $"{detik} detik";
        }

    }
}
