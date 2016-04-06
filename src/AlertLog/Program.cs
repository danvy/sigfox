using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace AlertLog
{
    class Program
    {
        static bool breaking = false;
        static void Main(string[] args)
        {
            var eventHubName = "alert";
            var consumerGroup = "log";
            var busConnectionString = ConfigurationManager.ConnectionStrings["SigfoxDemoAlertListener"].ConnectionString;
            var storageConnectionString = ConfigurationManager.ConnectionStrings["SigfoxDemoStorage"].ConnectionString;
            Console.CancelKeyPress += Console_CancelKeyPress;
            var eventHubClient = EventHubClient.CreateFromConnectionString(busConnectionString, eventHubName);
            if (consumerGroup == null)
                consumerGroup = eventHubClient.GetDefaultConsumerGroup().GroupName;
            var eventProcessorHost = new EventProcessorHost("AlertLogProcessor", eventHubClient.Path,
                consumerGroup, busConnectionString, storageConnectionString, eventHubName.ToLowerInvariant());
            eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>().Wait();
            while (true)
            {
                Console.WriteLine("Waiting for new messages " + DateTime.UtcNow);
                Thread.Sleep(1000);
                if (breaking)
                    break;
            }
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            breaking = true;
        }
    }
}
