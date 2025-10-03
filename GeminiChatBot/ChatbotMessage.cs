using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using GeminiChatBot.Services;
using GeminiChatBot.Helper;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Globalization;

namespace GeminiChatBot
{
    public class ChatbotMessage
    {
        private static string _cachedEncodedPdf = null;
        private static DateTime _lastCacheTime;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        private static IChatbotService _chatBotService;
        private static ISholatService _sholatService;
        private static IGoogleDriveService _googleDriveService;
        private static IConfiguration _configuration;

        private static string SourceResponse = string.Empty;
        static ChatbotMessage()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _chatBotService = new ChatbotService(_configuration);
            _sholatService = new SholatService(_configuration);
            //_googleDriveService = new GoogleDriveService();
        }

        public static async Task sentMessage(string prompt)
        {
            //await _googleDriveService.InitializeAsync();

            //string folderId = "1Rv9gKEqSblYb9YevE00YYdHVBHlt2IuC";
            //string downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), "GoogleDrive");
            //Directory.CreateDirectory(downloadFolder);

            //var files = await _googleDriveService.ReadAllFilesAsync(folderId, downloadFolder);

            //foreach (var file in files)
            //{
            //    Console.WriteLine($"Name: {file.Name}, Size: {file.Size}, Type: {file.MimeType}");
            //    if (!string.IsNullOrEmpty(file.Content))
            //    {
            //        Console.WriteLine($"--- Content Preview ---\n{file.Content[..Math.Min(200, file.Content.Length)]}\n");
            //    }
            //}

            //return;

            SourceResponse = string.Empty;
            var startTime = DateTime.Now;
            try
            {
                var listTag = string.Empty;
                var promptData = string.Empty;

                string geminiVersion = _configuration["Config:geminVersi"];
                string googleApiKey = _configuration["Config:googleApiKey"];

                string[] provisionsArray = _configuration.GetSection("Config:provision").Get<string[]>();

                string provision = string.Join(Environment.NewLine, provisionsArray);

                var partsData = Array.Empty<object>();
                var respone = string.Empty;

                //Console.WriteLine($"Waktu setelah greetings: {(DateTime.Now - startTime).TotalSeconds} detik");
                string encodedStringData = string.Empty;

                var shalatWords = new List<string> { "sholat", "salat", "shalat", "solat", "shlht" };
                var jadwalWowrds = new List<string> { "jadwal" };
                bool isShalat = shalatWords.Any(shalatWord => prompt.Contains(shalatWord, StringComparison.OrdinalIgnoreCase));
                bool isJadwal = jadwalWowrds.Any(jadwalWowrds => prompt.Contains(jadwalWowrds, StringComparison.OrdinalIgnoreCase));

                var kotaName = string.Empty;
                var jadwalSholatContent = string.Empty;
                if (isShalat && isJadwal)
                {
                    kotaName = await _sholatService.ExtractKotaNameDapper(prompt);

                    if (!string.IsNullOrEmpty(kotaName))
                    {
                        var date = DateTime.Now;

                        provision += $" Jika pertanyaan mengenai jadwal shalat tolong jawab dan filter data berdasarkan nama kota, tanggal dan bulan, dan hanya menyediakan jawaban untuk bulan saat ini, semisal menanyakan hari ini, kemarin, besok itu masih bisa, yang terpenting tidak lebih dari 30 hari atau 1 bulan kedepan, jika tidak ada menentukan kapan jadwal yang ditanyakan secara default ambil untuk jadwal hari ini, dan selalu infokan tanggalnya. Date sekarang: {date.ToString("dd/MM/yyyy")}. Jika tidak menemukan jawaban, tolong jangan infokan bahwa anda memiliki data propinsi, kota, dan lain lain.";

                    }
                    else
                    {
                        Console.WriteLine("Maaf, saya tidak dapat menemukan nama kota dalam pertanyaan Anda. Silakan sebutkan nama kota untuk mendapatkan jadwal shalat.");
                        return;
                    }

                    jadwalSholatContent = await _sholatService.GetJadwalSholatByKotaNameAsCsv(kotaName, true);

                    if (string.IsNullOrEmpty(jadwalSholatContent))
                    {
                        //TODO - Diupdate Text Response
                        Console.WriteLine($"Maaf, saya tidak memiliki data jadwal shalat untuk {kotaName} pada bulan ini.");
                        return;
                    }

                    //TODO - Coba untuk tidak diencoded
                    encodedStringData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jadwalSholatContent));

                    partsData = new object[]
                    {
                        new { inline_data = new { mime_type = "text/csv", data = encodedStringData } },
                        new { text = $"Pertanyaan: {prompt}\n\n{provision}" }
                    };
                }
                else
                {



                    var matchedDataLinks = (await _chatBotService.GetMatchedDataLinksAsync(prompt)).ToList();
                    var matchedDataFiles = (await _chatBotService.GetMatchedDataFilesAsync(prompt)).ToList();

                    //Console.WriteLine($"Matched Links: {string.Join(", ", matchedDataLinks)}");
                    //Console.WriteLine($"Matched Files: {string.Join(", ", matchedDataFiles)}");

                    //Console.WriteLine($"Waktu setelah GetMatchedDataLinksAsync: {(DateTime.Now - startTime).TotalSeconds} detik");

                    //Read to PDF Files if not matched some data shalat or link docs
                    //TODO
                    //1. Can be read to .docx, .xlsx
                    if ((matchedDataLinks == null || !matchedDataLinks.Any()) && (matchedDataFiles == null || !matchedDataFiles.Any()))
                    {
                        SourceResponse = $"\n_Source Files: Data Chatbot_";
                        encodedStringData = await GetCombinedPdfBase64Async(_configuration);
                        partsData = new object[]{
                                        new { inline_data = new { mime_type = "application/pdf", data = encodedStringData } },
                                        new { text = $"Pertanyaan: {prompt}\n\n{provision}" }
                        };
                    }
                    else
                    {
                        if ((matchedDataLinks == null || matchedDataLinks.Count == 0) && (matchedDataFiles == null || matchedDataFiles.Count == 0))
                        {
                            Console.WriteLine("Maaf, saya belum menemukan jawabannya. Silakan ajukan pertanyaan seputar aplikasi atau layanan Maslam.");
                            return;
                        }

                        var contentFiles = string.Empty;
                        foreach (var item in matchedDataLinks)
                        {
                            SourceResponse += $"\n_Source File: {await GetGoogleDocTitleAsync(item)}_";


                            var googleContent = await GoogleDocHelper.GetGoogleDocContentAsync(item, kotaName);

                            contentFiles += "\n\n" + googleContent;
                        }

                        foreach (var item in matchedDataFiles)
                        {
                            SourceResponse += $"\n_Source File: {item}_";

                            var contentFile = await GetDocumentTextAsync(item);

                            contentFiles += "\n\n" + contentFile;
                        }

                        partsData = new[] { new { text = $"Data: {contentFiles}\n\nPertanyaan: {prompt} berdasarkan data yang saya berikan. \n\n{provision}" } };

                    }

                    //Console.WriteLine($"Waktu setelah GetGoogleDocContentAsync: {(DateTime.Now - startTime).TotalSeconds} detik");

                }

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

                // Send the POST request

                //Console.WriteLine($"Waktu sebelum process gemini: {(DateTime.Now - startTime).TotalSeconds} detik");

                string jsonPayload = JsonSerializer.Serialize(payload);
                //Console.WriteLine(jsonPayload);

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5); // Increase to 5 minutes
                    var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{geminiVersion}:generateContent?key={googleApiKey}")
                    {
                        Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                    };

                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    //Console.WriteLine($"HTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");

                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        string retryAfterHeader = response.Headers.TryGetValues("Retry-After", out var values) ? values.FirstOrDefault() : null;

                        string pesanUntukPengguna;

                        string FormatWaktu(double totalSeconds)
                        {
                            int totalDetik = (int)Math.Ceiling(totalSeconds);
                            int jam = totalDetik / 3600;
                            int menit = (totalDetik % 3600) / 60;
                            int detik = totalDetik % 60;

                            if (jam > 0)
                            {
                                return $"{jam} jam {menit} menit {detik} detik";
                            }
                            else if (menit > 0)
                            {
                                return $"{menit} menit {detik} detik";
                            }
                            else
                            {
                                return $"{detik} detik";
                            }
                        }

                        double? waitSeconds = null;

                        if (int.TryParse(retryAfterHeader, out int retryAfterSeconds))
                        {
                            waitSeconds = retryAfterSeconds;
                        }
                        else
                        {
                            using var doc = JsonDocument.Parse(responseBody);
                            foreach (var detail in doc.RootElement.GetProperty("error").GetProperty("details").EnumerateArray())
                            {
                                if (detail.TryGetProperty("@type", out var typeEl) &&
                                    typeEl.GetString() == "type.googleapis.com/google.rpc.RetryInfo" &&
                                    detail.TryGetProperty("retryDelay", out var retryDelayEl))
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

                        if (waitSeconds.HasValue)
                        {
                            pesanUntukPengguna = $"Mohon maaf, permintaan anda saat ini melebihi batas. Silakan coba lagi dalam {FormatWaktu(waitSeconds.Value)}.";
                        }
                        else
                        {
                            pesanUntukPengguna = "Mohon maaf, permintaan anda saat ini melebihi batas. Silakan coba lagi beberapa saat lagi.";
                        }

                        Console.WriteLine(pesanUntukPengguna);
                        return;
                    }

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
                {
                    respone = "Maaf, saya belum menemukan jawabannya. Silakan ajukan pertanyaan seputar aplikasi atau layanan Maslam.";
                    Console.WriteLine(respone);
                    return;
                }

                var responseArray = respone.Split("$$^^&&");

                Console.WriteLine(respone); //Response
                if (!string.IsNullOrEmpty(SourceResponse))
                {
                    Console.WriteLine(SourceResponse); //Response
                }

                var endTime = DateTime.Now;
                var elapsedTime = endTime - startTime;
                //Console.WriteLine($"Total respons: {elapsedTime.TotalSeconds} detik");

            }
            catch (Exception ex)
            {
                //Console.WriteLine("err : " + ex.Message + ", Sorry please ask again.");
                Console.WriteLine("Maaf, terjadi kesalahan dalam memproses permintaan Anda. Silakan coba lagi. err : " + ex.Message);
                //Console.WriteLine("Mohon sistem tidak ");
            }
        }

        public static async Task<string> GetGoogleDocTitleAsync(string docLink)
        {
            if (string.IsNullOrWhiteSpace(docLink))
                throw new ArgumentException("Google Docs link is required.", nameof(docLink));

            using var http = new HttpClient();
            var html = await http.GetStringAsync(docLink);

            // Extract content between <title> and </title>
            var match = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.Singleline);
            if (!match.Success)
                throw new InvalidOperationException("Could not extract document title.");

            // Google Docs adds " - Google Docs" to the end of titles, so trim it
            var title = match.Groups[1].Value.Replace(" - Google Docs", "").Trim();
            return title;
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
            string folderPath = Path.Combine(AppContext.BaseDirectory, "DataManual");
            string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf").ToArray();

            CombinePdfs(pdfFiles, outputPdf);

            _cachedEncodedPdf = Convert.ToBase64String(await File.ReadAllBytesAsync(outputPdf));
            _lastCacheTime = DateTime.UtcNow;

            File.Delete(outputPdf);

            return _cachedEncodedPdf;
        }

        public static async Task<string> GetDocumentTextAsync(string filename)
        {
            string folderPath = Path.Combine(AppContext.BaseDirectory, "DataFiles");
            string filePath = Path.Combine(folderPath, filename);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            switch (ext)
            {
                case ".txt":
                    return await File.ReadAllTextAsync(filePath);

                case ".pdf":
                    var sb = new StringBuilder();
                    await Task.Run(() =>
                    {
                        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath))
                        {
                            foreach (Page page in pdf.GetPages())
                            {
                                sb.AppendLine(page.Text);
                            }
                        }
                    });
                    return sb.ToString();

                case ".docx":
                    throw new NotSupportedException(
                        ".docx is a ZIP-based format; use OpenXML SDK or another library to extract text.");

                default:
                    throw new NotSupportedException($"Unsupported file type: {ext}");
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

    }
}
