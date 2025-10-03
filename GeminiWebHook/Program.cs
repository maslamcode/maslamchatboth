using GeminiChatBot;
using GeminiWebHook;
using MaslamLibrary.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Webhook Verification
app.MapGet("/webhook", async (HttpContext context) =>
{
    var query = context.Request.Query;

    Console.WriteLine("Received Webhook Data: " + query);
    context.Response.StatusCode = 200;
});

// Handle Incoming WhatsApp Messages
app.MapPost("/webhook", async (HttpContext context) =>
{
    try
    {
        Console.WriteLine(DateTime.Now.ToString() + " Start");
        using var reader = new StreamReader(context.Request.Body);
        string requestBody = await reader.ReadToEndAsync();

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(requestBody);
        string displayName = jsonElement.GetProperty("contact").GetProperty("displayName").GetString();
        var msisdn = jsonElement.GetProperty("message").GetProperty("from").ToString();
        string messageText = jsonElement.GetProperty("message").GetProperty("content").GetProperty("text").GetString();
        bool is_from_me = msisdn == "+6281235022976";
        Console.WriteLine("msisdn: " + msisdn);
        Console.WriteLine("is_from_me: " + is_from_me);

        if (!is_from_me)
        {
            //Console.WriteLine("Tag: " + tags);
            //Console.WriteLine("Received Webhook Raw Data: " + requestBody);
            Console.WriteLine("Received Webhook Data: " + msisdn + ":" + messageText);
            var respone = await ChatbotMessage.sentMessage(messageText);
            IMessageBird watzap = new MessageBird();
            var responeWA = await watzap.sendMessage(msisdn, respone);
            Console.WriteLine("Respone AI: " + respone);
            Console.WriteLine("Respone WA: " + JsonSerializer.Serialize(responeWA));

        }
        Console.WriteLine(DateTime.Now.ToString() + " End");
        context.Response.StatusCode = 200;
    }
    catch(Exception ex)
    {

        Console.WriteLine("Exception caught: " + ex.Message);

        // Get the stack trace and extract the line number
        var stackTrace = new System.Diagnostics.StackTrace(ex, true);
        var frame = stackTrace.GetFrame(0); // Get the first frame
        int lineNumber = frame.GetFileLineNumber();

        // Output the file name and line number
        Console.WriteLine("Error occurred in file: " + frame.GetFileName());
        Console.WriteLine("Error on line: " + lineNumber);
        context.Response.StatusCode = 500;
    }

});

app.Run();
