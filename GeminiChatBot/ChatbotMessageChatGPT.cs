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
using static Google.Cloud.AIPlatform.V1.ReadFeatureValuesResponse.Types.EntityView.Types;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GeminiChatBot
{
    public class ChatbotMessageChatGPT
    {
        public static async Task sentMessage(string prompt)
        {
            using (var context = new MyDbContext())
            {

                var greetings = new List<string> { "hai", "halo", "salam", "asslamualaikum", "selamat pagi", "selamat siang", "selamat malam", "apa kabar" };
                var who = new List<string> { "siapa", "anda" };

                // Periksa apakah input termasuk sapaan
                bool isGreeting = greetings.Any(greeting => prompt.Contains(greeting, StringComparison.OrdinalIgnoreCase));
                if (isGreeting)
                {
                    var gretingRespone = await context.ChatbotResponses.Where(x => x.type == 1).Select(x => x.tag_message).ToListAsync();
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
                    var sayHai = await context.ChatbotResponses.Where(x => x.type == 2).OrderBy(x => x.order).Select(x => x.tag_message).ToListAsync();

                    foreach (var messge in sayHai)
                    {
                        Console.WriteLine(messge);
                        Console.WriteLine("");
                    }
                    return;
                }
                if (!isGreeting)
                {

                    var listTag = string.Join(", ", await context.Chatbot
                        .Select(x => (x.tag_message ?? "").ToLower())
                        .ToListAsync());

                    var respone = string.Empty;
                    // Paths to PDF files

                    string outputPdf = "combined_pdf.pdf";      // Path to the output PDF
                    const string ApiUrl = "https://api.openai.com/v1/chat/completions";

                    // Combine PDFs in the folder
                    string folderPath = AppContext.BaseDirectory;
                    string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
                    CombinePdfs(pdfFiles, outputPdf);
                    string encodedPdf = Convert.ToBase64String(await File.ReadAllBytesAsync(outputPdf));

                    var requestBody = new
                    {
                        model = "gpt-4",
                        messages = new[]
                       {
                            new { role = "system", content = @"Gunakan informasi dari PDF ini untuk menjawab pertanyaan pengguna dan awali jawaban dengan 'sesuai dengan sumber yang saya punya',jika tidak ditemukan baru menjawab dari informasi umum.
                                                               Jawab dengan Bahasa Indonesia."},
                                                            
                            new { role = "user", content = $"Pertanyaan: {prompt}\n\nBerikut adalah PDF dalam format Base64:\n{encodedPdf}" }
                        }
                    };
                    string jsonPayload = JsonSerializer.Serialize(requestBody);
                    //Console.WriteLine   (jsonPayload);
                    // Send the POST request

                    using (HttpClient httpClient = new HttpClient())
                    {

                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer 1234");
                        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}")
                        {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                        };

                        HttpResponseMessage response = await httpClient.SendAsync(request);

                        // Read and output the response
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Optionally parse and extract specific parts of the response
                        // Parse JSON using JsonDocument
                        using JsonDocument doc = JsonDocument.Parse(responseBody);

                        // Get root element
                        JsonElement root = doc.RootElement;

                        // Navigate to choices[0].message.content
                        if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                        {
                            JsonElement firstChoice = choices[0];

                            if (firstChoice.TryGetProperty("message", out JsonElement message) &&
                                message.TryGetProperty("content", out JsonElement content))
                            {
                                Console.WriteLine(content.GetString());
                            }
                        }
                    }
                    Console.WriteLine(respone);

                    // Clean up the downloaded PDF
                    File.Delete(outputPdf);

                }

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
    }
}
