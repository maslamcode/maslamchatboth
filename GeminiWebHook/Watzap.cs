using GeminiChatBot;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MaslamLibrary.Helper
{
    public class Watzap : IWatzap
    {
        private static string _api_key = "EPSQLEWYW7ZCDKHP";
        public async Task<WatzapModel> SendWA(string _arg_number_key, string _arg_no_wa, string _arg_message)
        {
            using (var context = new MyDbContext())
            {
                if (_arg_number_key == string.Empty)
                {
                    var query = context.Configs.FirstOrDefault(x => x.kategori== "otp" && x.param == "otp_number_key_2");
                    _arg_number_key = query?.value ?? string.Empty; ;
                }

            }
            #region Cara 1
            //var client = new HttpClient();
            //var request = new HttpRequestMessage(HttpMethod.Post, "https://api.watzap.id/v1/send_message");
            //var content = new StringContent($"{{\"api_key\": \"{_api_key}\", \"number_key\": \"{_arg_number_key}\", \"phone_no\": \"{_arg_no_wa}\", \"message\": \"{_arg_message}\"}}", null, "application/json");
            //request.Content = content;
            //var response = await client.SendAsync(request);
            //response.EnsureSuccessStatusCode();

            //var responseContent = await response.Content.ReadAsStringAsync();
            //var jsonResponse = JsonConvert.DeserializeObject<WatzapModel>(responseContent);

            //return jsonResponse;
            #endregion

            #region Cara 2

            var dataSending = new
            {
                api_key = _api_key,
                number_key = _arg_number_key,
                phone_no = _arg_no_wa,
                message = _arg_message
            };

            // URL API
            string url = "https://api.watzap.id/v1/send_message";

            // Membuat HttpClient
            using (HttpClient client = new HttpClient())
            {

                // Mengatur konten request
                string json = System.Text.Json.JsonSerializer.Serialize(dataSending);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                // Mengirim request POST
                HttpResponseMessage response = await client.PostAsync(url, content);

                // Membaca respons
                string responseContent = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<WatzapModel>(responseContent);

                return jsonResponse;
            }
            #endregion

        }
    }
}
