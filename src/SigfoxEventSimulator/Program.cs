using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SigfoxEventSimulator
{
    class Program
    {
        static Random rnd = new Random();
        static bool breaking = false;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            var data = new DeviceData();
            data.Device = "SIMUL001";
            var sas = "SharedAccessSignature sr=https%3a%2f%2fsigfox.servicebus.windows.net%2fsigfox%2fpublishers%2fsimul001%2fmessages&sig=N0ektKSEMjBzgyK49AKGxkneN14MiFDjvKknh49Der4%3d&se=1450191638&skn=device";
            var serviceNamespace = "sigfox";
            var hubName = "sigfox";
            var url = string.Format("{0}/publishers/{1}/messages", hubName, data.Device);
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(string.Format("https://{0}.servicebus.windows.net/", serviceNamespace))
            };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sas);
            var i = 1;
            while (!breaking)
            {
                data.Temp = 19 + rnd.Next(-2, 2);
                data.Time = DateTime.Now;
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                    var response = httpClient.PostAsync(url, content).Result;
                    Console.WriteLine(string.Format("{0} - Temp={1}, Response={2}", i, data.Temp, response.StatusCode));
                }
                catch
                {

                }
                Thread.Sleep(100);
            }
        }
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            breaking = true;
        }
    }
}
