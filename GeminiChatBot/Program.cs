using GeminiChatBot;
using GeminiChatBot.Models;
using GeminiChatBot.Services;
using Microsoft.Extensions.Configuration;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("upload", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Please provide a file path for upload.");
                        return;
                    }

                    string filePath = args[1];
                    await UploadFile(filePath);
                }
                else if (args[0].Equals("group-bulk-insert", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: group-bulk-insert '<jsonString>'");
                        return;
                    }

                    string json = args[1];
                    var groups = JsonSerializer.Deserialize<List<ChatBotGroupModel>>(json);

                    if (groups == null || groups.Count == 0)
                    {
                        Console.WriteLine("No groups provided.");
                        return;
                    }

                    var service = new ChatBotGroupService(new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build());

                    int totalInserted = 0;

                    foreach (var g in groups)
                    {
                        g.chatbot_group_id = Guid.NewGuid();
                        g.is_receive_broadcast = false;
                        g.created_date = DateTime.UtcNow;
                        g.last_updated = DateTime.UtcNow;
                        g.rowversion = DateTime.UtcNow;

                        totalInserted += await service.InsertGroupAsync(g);
                    }

                    Console.WriteLine($"Bulk insert completed. {totalInserted} groups inserted.");
                }
                else
                {
                    string prompt = args[0];
                    await ChatBothMessage.sentMessage(prompt);
                }
            }
            else
            {
                while (true)
                {
                    Console.WriteLine("Masukkan prompt (ketik 'exit' untuk keluar):");
                    string prompt = Console.ReadLine() ?? "";

                    if (prompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        break;

                    await ChatBothMessage.sentMessage(prompt);
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static async Task UploadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("❌ File not found.");
            return;
        }

        using (var client = new HttpClient())
        using (var form = new MultipartFormDataContent())
        using (var fileStream = File.OpenRead(filePath))
        {
            client.DefaultRequestHeaders.Add("x-api-key", "TCH0qIeozGfEkHGOSZuaYJaI3GKjylsnjnwiFMRPmltSsPRbhpyBatvzhYeHco9NnZXSxp628cAZrx5EkInTUqOb7LXBNkECgZFtJDnt07mVyarrAGwGH4W37cKzlSi3");

            var streamContent = new StreamContent(fileStream);
            form.Add(streamContent, "file", Path.GetFileName(filePath));

            HttpResponseMessage response = await client.PostAsync("http://172.104.163.223:3000/upload", form);

            string result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✅ Response: {result}");
        }
    }

    //static async System.Threading.Tasks.Task Main(string[] args)
    //{
    //    //args = new string[2];
    //    //args[1] = "perbedaan distribusi ziswaf dan pengeluaran operasional?";
    //    if (args.Length == 2)
    //    {
    //        string variable = args[0];  // First argument (number)
    //        string text = args[1];      // Second argument (string)

    //        //Console.WriteLine($"Received variable: {variable}");
    //        //Console.WriteLine($"Received string: {text}");
    //        await ChatBothMessage.sentMessage(text);
    //    }
    //    else
    //    {
    //        Console.WriteLine("Expected two arguments.");
    //    }
    //}

}
