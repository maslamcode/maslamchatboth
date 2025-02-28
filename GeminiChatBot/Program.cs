using GeminiChatBot;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        //args = new string[2];
        //args[1] = "perbedaan distribusi ziswaf dan pengeluaran operasional?";
        if (args.Length == 2)
        {
            string variable = args[0];  // First argument (number)
            string text = args[1];      // Second argument (string)

            //Console.WriteLine($"Received variable: {variable}");
            //Console.WriteLine($"Received string: {text}");
            await ChatBothMessage.sentMessage(text);
        }
        else
        {
            Console.WriteLine("Expected two arguments.");
        }
    }

}
