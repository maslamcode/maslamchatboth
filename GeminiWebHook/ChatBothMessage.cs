using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GeminiChatBot
{
    public class ChatBothMessage
    {
        public static async Task<string> sentMessage(string prompt)
        {
            string respone = string.Empty;
            using (var context = new MyDbContext())
            {
                var greetings = new List<string> { "hai", "halo", "salam","asslamualaikum" ,"selamat pagi", "selamat siang", "selamat malam", "apa kabar" };
                var who = new List<string> { "siapa", "anda"};

                // Periksa apakah input termasuk sapaan
                bool isGreeting = greetings.Any(greeting => prompt.Contains(greeting, StringComparison.OrdinalIgnoreCase));
                if (isGreeting)
                {
                    var gretingRespone = await context.ChatBothResponses.Where(x => x.type == 1).Select(x => x.tag_message).ToListAsync();
                    if (prompt.Contains("salam") || prompt.Contains("asslamualaikum"))
                        respone +=("wa'alaykumsalam wr wb");
                    foreach (var messge in gretingRespone)
                    {
                        respone += messge;
                        respone += "";
                    }
                    return respone;
                }
                if (prompt.Contains("siapa") && (prompt.Contains("anda") || prompt.Contains("kamu")))
                {
                    var sayHai = await context.ChatBothResponses.Where(x => x.type == 2).OrderBy(x=>x.order).Select(x => x.tag_message).ToListAsync();

                    foreach (var messge in sayHai)
                    {
                        respone += messge;
                        respone += "";
                    }
                    return respone;
                }
                if (!isGreeting)
                {

                    var listTag = string.Join(", ", await context.ChatBoths
                        .Select(x => (x.tag_message ?? "").ToLower())
                        .ToListAsync());

                    respone = string.Empty;
                    // Paths to PDF files
                    string outputPdf = "combined_pdf.pdf";      // Path to the output PDF
                    string googleApiKey = "AIzaSyBRlaqXLOiqCgW0u3n_s66dafgWMWC_SGo"; // Replace with your API key

                    // Combine PDFs in the folder
                    string folderPath = AppContext.BaseDirectory;
                    string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
                    CombinePdfs(pdfFiles, outputPdf);
                    string encodedPdf = Convert.ToBase64String(await File.ReadAllBytesAsync(outputPdf));


                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts =  new object[]
                                    {
                                        new { inline_data = new { mime_type = "application/pdf", data = encodedPdf } },
                                        //new { text = prompt + @" (utamakan berdasarkan file pdf)
                                        //                         (jika pertanyaan tidak ada hubunganya dengan '"+listTag+@"', maka jawab dengan 'Kang SAMI tidak yakin dengan jawaban untuk pertanyaan tersebut.'. jika ditemukan, jawaban dimuali dengan 'menurut Kang SAMI ,')
                                        //                         (Jawab dengan Bahasa Indonesia)" }
                                        new { text = prompt + @" (utamakan berdasarkan file pdf , tidak perlu menjawab 'berdasarkan file PDF yang Anda berikan', jika tidak ditemukan baru jawab dengan 'Sebagai bagian dari Maslam, saya hanya dirancang untuk menjawab informasi terkait Maslam. Silakan tanyakan hal-hal seputar digitalisasi manajemen masjid/lembaga, fitur aplikasi Maslam, atau layanan kami.')
                                        (Jawab dengan Bahasa Indonesia)" }
                                    }
                            }
                        }
                    };
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    //Console.WriteLine   (jsonPayload);
                    // Send the POST request

                    using (HttpClient httpClient = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={googleApiKey}")
                        {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                        };

                        HttpResponseMessage response = await httpClient.SendAsync(request);

                        // Read and output the response
                        string responseBody = await response.Content.ReadAsStringAsync();

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

                    // Clean up the downloaded PDF
                    File.Delete(outputPdf);

                }

            }
            return respone;
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
    }
}
