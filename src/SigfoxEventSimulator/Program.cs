using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SigfoxLib;

namespace SigfoxEventSimulator
{
    class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int key);
        private const int KeyPressed = 0x8000;
        static Random rnd = new Random();
        static bool breaking = false;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            var data = new Container();
            data.DeviceId = "d1";
            data.Temp = 19 + rnd.Next(-2, 2);
            var sas = "SharedAccessSignature sr=https%3A%2F%2Fsigfox.servicebus.windows.net%2Fsigfox%2Fpublishers%2Fd1%2Fmessages&sig=80%2Fof8wW6u7Ne8Z6NQLqOjlr5UmdFnKrzt%2FOzXzj93Q%3D&se=1462320741&skn=device";
            var serviceNamespace = "sigfox";
            var hubName = "sigfox";
            var url = string.Format("{0}/publishers/{1}/messages", hubName, data.DeviceId);
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(string.Format("https://{0}.servicebus.windows.net/", serviceNamespace))
            };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sas);
            var i = 1;
            while (!breaking)
            {
                if (IsKeyDown(0x26))
                {
                    data.Temp += 1;
                }
                else if (IsKeyDown(0x28))
                {
                    data.Temp -= 1;
                }
                data.LastUpdate = DateTime.Now;
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
        public static bool IsKeyDown(int key)
        {
            return (GetKeyState((int)key) & KeyPressed) != 0;
        }
    }
}
