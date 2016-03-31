using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace DeviceSimulator
{
    class Program
    {
        private static bool breaking;
        private static Random rnd = new Random();
        private static HttpClient httpClient;
        private static string sas = "SharedAccessSignature sr=https%3A%2F%2Fsigfox.servicebus.windows.net%2Fsigfox%2Fpublishers%2Fd1%2Fmessages&sig=80%2Fof8wW6u7Ne8Z6NQLqOjlr5UmdFnKrzt%2FOzXzj93Q%3D&se=1462320741&skn=device";
        private static string serviceNamespace = "sigfox";
        private static string hubName = "sigfox";
        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            Devices.Init();
            httpClient = new HttpClient()
            {
                BaseAddress = new Uri(string.Format("https://{0}.servicebus.windows.net/", serviceNamespace))
            };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", sas);
            //var client = EventHubClient.CreateFromConnectionString("Endpoint=http://sigfox.servicebus.windows.net;SharedSecretIssuer=sigfox;SharedSecretValue=nyXvyZezWGZE5KX9eUYVJ7v/XHVG/l8uBB0cYr7zt0sGXTNq0W6JggC6jxDQSySQXIongfoXqrtA2CaXomc/Vg==", "sigfox");
            while (!breaking)
            {
                foreach (var container in Devices.Instance)
                {
                    if (breaking)
                        break;
                    if (rnd.Next(10) > 7)
                    {
                        Device.Update();
                        try
                        {
                            var rc = PostEvent(container);
                            Console.WriteLine("{0} {1} {2}", container.Id, container.Fullness, rc);
                            //client.Send(data);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
            
        private static bool PostEvent(Device device)
        {
            try
            {
                var json = JsonConvert.SerializeObject(device);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = string.Format("{0}/publishers/{1}/messages", hubName, "d1");
                var response = httpClient.PostAsync(url, content).Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            breaking = true;
        }
    }
}
