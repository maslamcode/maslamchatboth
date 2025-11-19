using Chatbot.Service.Model.ChatbotGroup;
using Chatbot.Service.Services.ChatbotCharacter;
using Chatbot.Service.Services.ChatbotGroup;
using Chatbot.Service.Services.ChatbotNumber;
using Chatbot.Service.Services.ChatbotNumberTask;
using Chatbot.Service.Services.ChatbotTaskList;
using GeminiChatBot;
using Microsoft.Extensions.Configuration;
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
                    var groups = JsonSerializer.Deserialize<List<ChatbotGroupModel>>(json);

                    if (groups == null || groups.Count == 0)
                    {
                        Console.WriteLine("No groups provided.");
                        return;
                    }

                    var service = new ChatbotGroupService(new ConfigurationBuilder()
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
                else if (args[0].Equals("get-number-with-character", StringComparison.OrdinalIgnoreCase))
                {
                    var config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

                    var numberService = new ChatbotNumberService(config);
                    var characterService = new ChatbotCharacterService(config);
                    var numberTaskService = new ChatbotNumberTaskService(config);
                    var taskListService = new ChatbotTaskListService(config);

                    var numbers = await numberService.GetAllNumbersAsync();
                    var first = numbers.FirstOrDefault();

                    if (first == null)
                    {
                        Console.WriteLine("No chatbot number found.");
                        return;
                    }

                    var character = await characterService.GetCharacterByIdAsync(first.chatbot_character_id);

                    var numberTasks = await numberTaskService.GetAllTasksByNumberIdAsync(first.chatbot_number_id);

                    var taskListIds = numberTasks.Select(t => t.chatbot_task_list_id).Distinct().ToList();
                    var taskLists = new List<object>();

                    foreach (var id in taskListIds)
                    {
                        var tl = await taskListService.GetTaskListByIdAsync(id);
                        if (tl != null)
                        {
                            taskLists.Add(tl);
                        }
                    }

                    var combined = new
                    {
                        number = first,
                        character = character,
                        tasks = numberTasks,
                        taskLists = taskLists
                    };

                    var json = JsonSerializer.Serialize(combined, new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

                    Console.WriteLine($"__JSON_START__{json}__JSON_END__");

                }
                else if (args[0].Equals("whatsapp-connected", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Missing parameters: whatsapp-connected <phoneNumber> <whatsappId>");
                        return;
                    }

                    string phoneNumber = args[1];
                    string whatsappId = args[2];

                    var config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

                    var numberService = new ChatbotNumberService(config);

                    var numbers = await numberService.GetAllNumbersAsync();

                    if (numbers == null || !numbers.Any())
                    {
                        Console.WriteLine("No chatbot numbers found to update.");
                        return;
                    }

                    await numberService.UpdateAllNumbersAsync(phoneNumber, whatsappId);

                    Console.WriteLine($"Updated Nomor  : {phoneNumber}");
                    Console.WriteLine($"Updated WA ID  : {whatsappId}");
                }
                else
                {
                    string prompt = args[0];
                    await ChatbotMessage.sentMessage(prompt);
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

                    await ChatbotMessage.sentMessage(prompt);
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
    //        await ChatbotMessage.sentMessage(text);
    //    }
    //    else
    //    {
    //        Console.WriteLine("Expected two arguments.");
    //    }
    //}

}
