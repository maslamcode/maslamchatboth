using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using GeminiChatBot.Services;
using GeminiChatBot.Helper;

namespace GeminiChatBot
{
    public class ChatBothMessage
    {
        private static string _cachedEncodedPdf = null;
        private static DateTime _lastCacheTime;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        private static IChatBotService _chatBotService;
        private static ISholatService _sholatService;
        private static IConfiguration _configuration;
        static ChatBothMessage()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _chatBotService = new ChatBotService(_configuration);
            _sholatService = new SholatService(_configuration);
        }

        public static async Task sentMessage(string prompt)
        {
            var startTime = DateTime.Now;
            try
            {
                var listTag = string.Empty;
                var promptData = string.Empty;

                MyDbContext context = null;
                var responseGreetings = await _chatBotService.HandlePromptGreetingsAsync(prompt);
                if (!string.IsNullOrEmpty(responseGreetings))
                {
                    Console.WriteLine(responseGreetings);
                    return;
                }

                string geminiVersion = _configuration["Config:geminVersi"];
                string googleApiKey = _configuration["Config:googleApiKey"];

                var provision = "Ketentuan: Gunakan data yang telah disediakan. Jika tidak ditemukan jawab dengan dinamis bahwa hanya memiliki data untuk data tersebut, jika pertanyaan diluar konteks dan tidak ada dari data yang diberikan maka jawab 'Sebagai bagian dari Maslam, saya hanya dirancang untuk menjawab informasi terkait Maslam. Silakan tanyakan hal-hal seputar digitalisasi manajemen masjid/lembaga, fitur aplikasi Maslam, atau layanan kami.', berikan jawaban to the point tanpa bahasa seperti berdasarkan data yang anda berikan (Jawab dengan Indonesian)";
                var partsData = Array.Empty<object>();
                var respone = string.Empty;

                //Console.WriteLine($"Waktu setelah greetings: {(DateTime.Now - startTime).TotalSeconds} detik");
                string encodedStringData = string.Empty;

                var shalatWords = new List<string> { "sholat", "salat", "shalat", "solat" };
                bool isShalat = shalatWords.Any(shalatWord => prompt.Contains(shalatWord, StringComparison.OrdinalIgnoreCase));
                var kotaName = string.Empty;
                var jadwalSholatContent = string.Empty;
                if (isShalat)
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
                        Console.WriteLine($"Maaf, saya tidak memiliki data jadwal shalat untuk {kotaName} pada bulan ini.");
                        return;
                    }

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

                    //Console.WriteLine($"Waktu setelah GetMatchedDataLinksAsync: {(DateTime.Now - startTime).TotalSeconds} detik");

                    //Read to PDF Files if not matched some data shalat or link docs
                    //TODO
                    //1. Can be read to .docx, .xlsx
                    if ((matchedDataLinks == null || !matchedDataLinks.Any()))
                    {
                        encodedStringData = await GetCombinedPdfBase64Async(_configuration);
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
                            var googleContent = await GoogleDocHelper.GetGoogleDocContentAsync(item, kotaName);

                            googleContents += "\n\n" + googleContent;
                        }

                        partsData = new[] { new { text = $"Data: {googleContents}\n\nPertanyaan: {prompt} berdasarkan data yang saya berikan. \n\n{provision}" } };

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

            CombinePdfs(pdfFiles, outputPdf);

            _cachedEncodedPdf = Convert.ToBase64String(await File.ReadAllBytesAsync(outputPdf));
            _lastCacheTime = DateTime.UtcNow;

            File.Delete(outputPdf);

            return _cachedEncodedPdf;
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
