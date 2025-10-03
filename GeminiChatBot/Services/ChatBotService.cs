using Dapper;
using GeminiChatBot.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig.Content;

namespace GeminiChatBot.Services
{
    public class ChatbotService: IChatbotService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ChatbotService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection") ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
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

            return null;
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


    }
}
