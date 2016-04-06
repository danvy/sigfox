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

namespace DecoderJob
{
    class Program
    {
        static bool quit = false;
        public static void Main()
        {
            var eventHubName = "dispatch";
            var consumerGroup = "storage";
            var busConnectionString = ConfigurationManager.ConnectionStrings["SigfoxDemoDispatchListener"].ConnectionString;
            var storageConnectionString = ConfigurationManager.ConnectionStrings["SigfoxDemoStorage"].ConnectionString;
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
            var eventProcessorHost = new EventProcessorHost("StorageProcessor", eventHubClient.Path,
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
