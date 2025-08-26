using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Google.Cloud.AIPlatform.V1;

namespace GeminiChatBot
{
    public class ChatBothMessage
    {
        private static string _cachedEncodedPdf = null;
        private static DateTime _lastCacheTime;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
        public static async Task sentMessage(string prompt)
        {
            var startTime = DateTime.Now;
            try
            {
                var configuration = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", false, true)
                      .Build();

                var connectingString = configuration.GetConnectionString("PostgreSqlConnection");
                var gretingRespone = new List<string>();
                var sayHai = new List<string>();
                var listTag = string.Empty;
                var promptData = string.Empty;

                MyDbContext context = null;
                try
                {
                    context = new MyDbContext(connectingString);
                    gretingRespone = await context.ChatBothResponses.Where(x => x.type == 1).Select(x => x.tag_message).ToListAsync();
                    sayHai = await context.ChatBothResponses.Where(x => x.type == 2).OrderBy(x => x.order).Select(x => x.tag_message).ToListAsync();
                    //listTag = string.Join(", ", await context.ChatBoths
                    //   .Select(x => (x.tag_message ?? "").ToLower())
                    //   .ToListAsync());
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                var greetings = new List<string> { "hai", "halo", "salam", "asslamualaikum", "selamat pagi", "selamat siang", "selamat malam", "apa kabar" };
                var who = new List<string> { "siapa", "anda" };

                // Periksa apakah input termasuk sapaan
                bool isGreeting = greetings.Any(greeting => prompt.Contains(greeting, StringComparison.OrdinalIgnoreCase));
                if (isGreeting)
                {
                    if (prompt.Contains("salam") || prompt.Contains("asslamualaikum"))
                        Console.WriteLine("wa'alaykumsalam wr wb");
                    foreach (var messge in gretingRespone)
                    {
                        Console.WriteLine(messge);
                        Console.WriteLine("");
                    }
                    return;
                }
                if (prompt.Contains("siapa") && (prompt.Contains("anda") || prompt.Contains("kamu")))
                {

                    foreach (var messge in sayHai)
                    {
                        Console.WriteLine(messge);
                        Console.WriteLine("");
                    }
                    return;
                }

                if (prompt.Length < 5)
                {
                    Console.WriteLine("Pertanyaan terlalu singkat, silakan ajukan pertanyaan yang lebih jelas.");
                    return;
                }

                var provision = "Ketentuan: Gunakan data yang telah disediakan. Jika tidak ditemukan jawab dengan dinamis bahwa hanya memiliki data untuk data tersebut, jika pertanyaan diluar konteks dan tidak ada dari data yang diberikan maka jawab 'Sebagai bagian dari Maslam, saya hanya dirancang untuk menjawab informasi terkait Maslam. Silakan tanyakan hal-hal seputar digitalisasi manajemen masjid/lembaga, fitur aplikasi Maslam, atau layanan kami.', berikan jawaban to the point tanpa bahasa seperti berdasarkan data yang anda berikan (Jawab dengan Indonesian)";
                var partsData = Array.Empty<object>();
                if (!isGreeting)
                {
                    var respone = string.Empty;
                    string geminiVersion = configuration["Config:geminVersi"];
                    string googleApiKey = configuration["Config:googleApiKey"];
                    var selections = configuration.GetSection("DataLinkPromptSelection").Get<List<DataLinkPromptSelection>>();
                    var matchedDataLinks = new List<string>();

                    //Console.WriteLine($"Waktu setelah combine/cache data: {(DateTime.Now - startTime).TotalSeconds} detik");
                    string encodedStringData = string.Empty;

                    var shalatWords = new List<string> { "sholat", "salat", "shalat", "solat" };
                    bool isShalat = shalatWords.Any(shalatWord => prompt.Contains(shalatWord, StringComparison.OrdinalIgnoreCase));
                    var kotaName = string.Empty;
                    if (isShalat)
                    {
                        kotaName = await ExtractKotaName(prompt, context);

                        if (!string.IsNullOrEmpty(kotaName))
                        {
                            var date = DateTime.Now;
                           
                            provision += $" Jika pertanyaan mengenai jadwal shalat tolong jawab dan filter data berdasarkan nama kota, tanggal dan bulan, dan hanya menyediakan jawaban untuk bulan saat ini, semisal menanyakan hari ini, kemarin, besok itu masih bisa, yang terpenting tidak lebih dari 30 hari atau 1 bulan kedepan. Date sekarang: {date.ToString("dd/MM/yyyy")}. Jika tidak menemukan jawaban, tolong jangan infokan bahwa anda memiliki data propinsi, kota, dan lain lain.";

                        }
                        else
                        {
                            Console.WriteLine("Maaf, saya tidak dapat menemukan nama kota dalam pertanyaan Anda. Silakan sebutkan nama kota untuk mendapatkan jadwal shalat.");
                            return;
                        }

                    }

                    foreach (var selection in selections)
                    {
                        var keywords = selection.PromptWords
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        if (keywords.Any(k => prompt.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!matchedDataLinks.Contains(selection.Link))
                                matchedDataLinks.Add(selection.Link);
                        }
                    }


                    //Read to PDF Files if not matched some data shalat or link docs
                    if ((matchedDataLinks == null || !matchedDataLinks.Any()))
                    {
                        encodedStringData = await GetCombinedPdfBase64Async(configuration);
                        partsData = new object[]{
                                        new { inline_data = new { mime_type = "application/pdf", data = encodedStringData } },
                                        new { text = $"Pertanyaan: {prompt}\n\n{provision}" }
                        };
                    }
                    else
                    {
                        if (matchedDataLinks == null || matchedDataLinks.Count == 0)
                        {
                            Console.WriteLine("Maaf, saya belum menemukan jawabannya. Silakan ajukan pertanyaan seputar aplikasi atau layanan Maslam.");
                            return;
                        }

                        var googleContents = string.Empty;
                        foreach (var item in matchedDataLinks)
                        {
                            var googleContent = await GetGoogleDocContentAsync(item, kotaName);

                            googleContents += "\n\n" + googleContent;
                        }

                        partsData = new[] { new { text = $"Data: {googleContents}\n\nPertanyaan: {prompt} berdasarkan data yang saya berikan. \n\n{provision}" } };

                    }

                    //Console.WriteLine($"PartsData: {partsData[0]}");

                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = partsData
                            }
                        }
                    };

                    string jsonPayload = JsonSerializer.Serialize(payload);
                    //Console.WriteLine   (jsonPayload);
                    // Send the POST request

                    //Console.WriteLine($"Waktu sebelum process gemini: {(DateTime.Now - startTime).TotalSeconds} detik");

                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromMinutes(5); // Increase to 5 minutes
                        var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{geminiVersion}:generateContent?key={googleApiKey}")
                        {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                        };

                        HttpResponseMessage response = await httpClient.SendAsync(request);

                        //Console.WriteLine($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                        // Read and output the response
                        string responseBody = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine("responseBody:", responseBody);
                        // Optionally parse and extract specific parts of the response
                        using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
                        {
                            if (jsonDoc.RootElement.TryGetProperty("candidates", out JsonElement candidates))
                            {
                                foreach (JsonElement candidate in candidates.EnumerateArray())
                                {
                                    if (candidate.TryGetProperty("content", out JsonElement content) &&
                                        content.TryGetProperty("parts", out JsonElement contentParts))
                                    {
                                        foreach (JsonElement part in contentParts.EnumerateArray())
                                        {
                                            if (part.TryGetProperty("text", out JsonElement textElement))
                                            {
                                                string potentialResponse = textElement.GetString();
                                                if (!string.IsNullOrWhiteSpace(potentialResponse))
                                                {
                                                    respone = potentialResponse;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }

                    //respone += responseBody;
                    if (string.IsNullOrEmpty(respone))
                        respone = "Maaf, saya belum menemukan jawabannya. Silakan ajukan pertanyaan seputar aplikasi atau layanan Maslam.";
                    Console.WriteLine(respone);

                    var endTime = DateTime.Now;
                    var elapsedTime = endTime - startTime;
                    //Console.WriteLine($"Total respons: {elapsedTime.TotalSeconds} detik");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("err : " + ex.Message + ", Sorry please ask again.");
            }
        }

        // Method to combine two PDFs
        static void CombinePdfs(string[] pdfFiles, string outputPdf)
        {
            // Create a new output document
            using (PdfDocument outputDocument = new PdfDocument())
            {
                // Loop through each PDF file in the list
                foreach (var pdfFile in pdfFiles)
                {
                    // Open the current PDF file
                    PdfDocument inputDocument = PdfReader.Open(pdfFile, PdfDocumentOpenMode.Import);

                    // Add each page of the current PDF to the output document
                    for (int i = 0; i < inputDocument.PageCount; i++)
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }

                // Save the combined PDF to the specified output file
                outputDocument.Save(outputPdf);
            }
        }

        private static async Task<string> GetCombinedPdfBase64Async(IConfiguration configuration)
        {
            // If cache is still valid, just return it
            if (_cachedEncodedPdf != null && DateTime.UtcNow - _lastCacheTime < _cacheDuration)
                return _cachedEncodedPdf;

            string outputPdf = Path.Combine(Path.GetTempPath(), "combined_pdf.pdf");

            // Local PDFs
            string folderPath = Path.Combine(AppContext.BaseDirectory, "DataPDF");
            string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf").ToArray();

            // Online PDFs (Google Docs)
            var onlineSources = configuration.GetSection("DataGoogleDocsOnline").Get<string[]>();
            foreach (var source in onlineSources)
            {
                string localTempFile = Path.GetTempFileName();
                using var httpClient = new HttpClient();
                string url = source;

                if (url.Contains("docs.google.com/document"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
                    if (match.Success)
                    {
                        string docId = match.Groups[1].Value;
                        url = $"https://docs.google.com/document/d/{docId}/export?format=pdf";
                    }
                }

                byte[] pdfData = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(localTempFile, pdfData);

                pdfFiles = pdfFiles.Concat(new[] { localTempFile }).ToArray();
            }

            CombinePdfs(pdfFiles, outputPdf);

            _cachedEncodedPdf = Convert.ToBase64String(await File.ReadAllBytesAsync(outputPdf));
            _lastCacheTime = DateTime.UtcNow;

            File.Delete(outputPdf);

            return _cachedEncodedPdf;
        }

        public static async Task<string> GetGoogleDocContentAsync(string googleLink, string kotaName = null)
        {
            using var httpClient = new HttpClient();
            string url = googleLink;

            if (url.Contains("docs.google.com/document"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
                if (!match.Success)
                    throw new ArgumentException("Invalid Google Docs link");

                string docId = match.Groups[1].Value;

                url = $"https://docs.google.com/document/d/{docId}/export?format=txt";

                return await httpClient.GetStringAsync(url);
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
                    return csvContent;

                int month = DateTime.Now.Month;

                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1)
                    return csvContent;

                var header = lines[0].Split(',');
                var resultLines = new List<string> { string.Join(',', header) };

                int kotaIndex = Array.FindIndex(header, h => h.Equals("kota", StringComparison.OrdinalIgnoreCase));
                int bulanIndex = Array.FindIndex(header, h => h.Equals("bulan", StringComparison.OrdinalIgnoreCase));

                foreach (var line in lines.Skip(1))
                {
                    var cols = line.Split(',');
                    if (cols.Length != header.Length) continue;

                    bool matchKota = string.IsNullOrEmpty(kotaName) || cols[kotaIndex].Contains(kotaName, StringComparison.OrdinalIgnoreCase);
                    bool matchBulan = month == null || cols[bulanIndex].Trim() == month.ToString();

                    if (matchKota && matchBulan)
                    {
                        resultLines.Add(line);
                    }
                }

                return string.Join('\n', resultLines);
            }
            else
            {
                throw new ArgumentException("Unsupported Google link. Must be Docs or Sheets.");
            }
        }

        //public static async Task<string> GetGoogleDocContentAsync(string googleLink, string googleApiKey = null)
        //{
        //    using var httpClient = new HttpClient();
        //    string url = googleLink;

        //    // --- Google Docs ---
        //    if (url.Contains("docs.google.com/document"))
        //    {
        //        var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
        //        if (!match.Success)
        //            throw new ArgumentException("Invalid Google Docs link");

        //        string docId = match.Groups[1].Value;

        //        url = $"https://docs.google.com/document/d/{docId}/export?format=txt";
        //        return await httpClient.GetStringAsync(url);
        //    }

        //    // --- Google Sheets ---
        //    else if (url.Contains("docs.google.com/spreadsheets"))
        //    {
        //        var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
        //        if (!match.Success)
        //            throw new ArgumentException("Invalid Google Sheets link");

        //        string sheetId = match.Groups[1].Value;

        //        if (string.IsNullOrEmpty(googleApiKey))
        //        {
        //            url = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";
        //            return await httpClient.GetStringAsync(url);
        //        }

        //        string metaUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{sheetId}?key={googleApiKey}";
        //        string metaJson = await httpClient.GetStringAsync(metaUrl);

        //        using var doc = JsonDocument.Parse(metaJson);
        //        if (!doc.RootElement.TryGetProperty("sheets", out JsonElement sheets))
        //            throw new Exception("No sheets found in this spreadsheet.");

        //        var allContent = new StringBuilder();

        //        foreach (var sheet in sheets.EnumerateArray())
        //        {
        //            string title = sheet.GetProperty("properties").GetProperty("title").GetString();
        //            string gid = sheet.GetProperty("properties").GetProperty("sheetId").GetRawText();

        //            string csvUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";
        //            string csvContent = await httpClient.GetStringAsync(csvUrl);

        //            allContent.AppendLine($"--- Sheet: {title} ---");
        //            allContent.AppendLine(csvContent);
        //            allContent.AppendLine();
        //        }

        //        return allContent.ToString();
        //    }

        //    else
        //    {
        //        throw new ArgumentException("Unsupported Google link. Must be Docs or Sheets.");
        //    }
        //}




        public static async Task<string?> ExtractKotaName(string prompt, MyDbContext context)
        {
            var kotalist = await context.Kotas.Select(k => k.Nama).ToListAsync();

            foreach (var namakota in kotalist)
            {
                var overrideNamaKota = namakota.Replace("Kota", string.Empty).Replace("Kabupaten", string.Empty).TrimStart();

                if (prompt.Contains(overrideNamaKota, StringComparison.OrdinalIgnoreCase))
                {
                    return namakota;
                }
            }

            return null;
        }

        public class DataLinkPromptSelection
        {
            public string PromptWords { get; set; }
            public string Link { get; set; }
        }

    }
}
