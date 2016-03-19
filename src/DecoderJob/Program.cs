using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.IO;
using System.Configuration;
using System.Threading;

namespace DecoderJob
{
    class Program
    {
        static bool breaking = false;
        public static void Main()
        {
            var settings = ConfigurationManager.AppSettings;
            var eventHubName = settings["SourceEventHubName"];
            var consumerGroup = settings["SourceConsumerGroup"];
            var connections = ConfigurationManager.ConnectionStrings;
            var busConnectionString = connections["SourceBusConnectionString"].ConnectionString;
            var storageConnectionString = connections["SourceStorageConnectionString"].ConnectionString;
            Console.CancelKeyPress += Console_CancelKeyPress;
            var eventHubClient = EventHubClient.CreateFromConnectionString(busConnectionString, eventHubName);
            if (consumerGroup == null)
                consumerGroup = eventHubClient.GetDefaultConsumerGroup().GroupName;
            var eventProcessorHost = new EventProcessorHost(settings["EventProcessorName"], eventHubClient.Path,
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
