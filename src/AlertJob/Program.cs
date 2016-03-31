using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.IO;
using System.Configuration;
using System.Threading;
using Danvy.Azure;

namespace AlertJob
{
    class Program
    {
        static bool quit = false;
        public static void Main()
        {
            var settings = ConfigurationManager.AppSettings;
            var eventHubName = settings["EventHubName"];
            var consumerGroup = settings["ConsumerGroup"];
            var connections = ConfigurationManager.ConnectionStrings;
            var busConnectionString = connections["BusConnectionString"].ConnectionString;
            var storageConnectionString = connections["StorageConnectionString"].ConnectionString;
            if (!WebJobsHelper.RunAsWebJobs)
                Console.CancelKeyPress += Console_CancelKeyPress;
            EventHubClient eventHubClient = null;
            var retries = 3;
            while (retries > 0)
            {
                try
                {
                    retries--;
                    eventHubClient = EventHubClient.CreateFromConnectionString(busConnectionString, eventHubName);
                    retries = 0;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error opening source Event Hub: " + e.Message);
                    if (retries == 0)
                        throw;
                }
            }
            if (consumerGroup == null)
                consumerGroup = eventHubClient.GetDefaultConsumerGroup().GroupName;
            var eventProcessorHost = new EventProcessorHost(settings["EventProcessorName"], eventHubClient.Path,
                consumerGroup, busConnectionString, storageConnectionString, eventHubName.ToLowerInvariant());
            eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>().Wait();
            while (true)
            {
                if (WebJobsHelper.RunAsWebJobs)
                {
                    Thread.Sleep(50);
                }
                else
                {
                    Console.WriteLine("Waiting for new messages " + DateTime.UtcNow);
                    Thread.Sleep(1000);
                }
                if (quit || WebJobsHelper.NeedShutdown)
                    break;
            }
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            quit = true;
        }
    }
}
