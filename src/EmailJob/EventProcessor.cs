using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Danvy.Azure;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SendGrid;
using SensitLib;
using SigfoxDemoLib;
using StackExchange.Redis;

namespace DecoderJob
{
    public class EventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;
        private Web transportWeb;

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (!WebJobsHelper.RunAsWebJobs)
                Console.WriteLine(string.Format("Processor Shuting Down. Partition '{0}', Reason: '{1}'.",
                    partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
                await context.CheckpointAsync();
        }
        public Task OpenAsync(PartitionContext context)
        {
            if (!WebJobsHelper.RunAsWebJobs)
                Console.WriteLine(string.Format("EventProcessor initialization. Partition: '{0}', Offset: '{1}'",
                    context.Lease.PartitionId, context.Lease.Offset));
            partitionContext = context;
            var retries = 3;
            while (retries > 0)
            {
                try
                {
                    retries--;
                    var username = ConfigurationManager.AppSettings["SendGridUser"];
                    var pswd = ConfigurationManager.AppSettings["SendGridPass"];
                    var apiKey = ConfigurationManager.AppSettings["SendGridAPIKey"];
                    transportWeb = new Web(apiKey);
                    retries = 0;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error opening destination Event Hub: " + e.Message);
                    if (retries == 0)
                        throw;
                }
            }
            checkpointStopWatch = new Stopwatch();
            checkpointStopWatch.Start();
            return Task.CompletedTask;
        }
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    var jsonMessage = Encoding.UTF8.GetString(message.GetBytes());
                    if (!WebJobsHelper.RunAsWebJobs)
                        Console.WriteLine(string.Format("Message: {0}", jsonMessage));
                    AlertMessage messageObj = null;
                    try
                    {
                        messageObj = JsonConvert.DeserializeObject<AlertMessage>(jsonMessage);
                    }
                    catch
                    {

                    }
                    if (messageObj != null)
                    {
                        await SendEmailAsync(messageObj);
                        //await StoreInBlobAsync(messageObj.Device, jsonMessage);
                        //await StoreInDatabaseAsync(messageObj);
                        //await StoreInCacheAsync(messageObj.Device, jsonMessage);
                    }
                }
                if (this.checkpointStopWatch.Elapsed > TimeSpan.FromSeconds(10))
                {
                    await context.CheckpointAsync();
                    lock (this)
                    {
                        checkpointStopWatch.Reset();
                        checkpointStopWatch.Start();
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error getting messages:" + e.Message);
            }
        }

        private async Task SendEmailAsync(AlertMessage message)
        {
            if (message == null)
                return;
            if (string.IsNullOrEmpty(message.Device))
                return;
            var email = new SendGridMessage();
            email.From = new MailAddress("alex.danvy@microsoft.com", "Alex Danvy (Sigfox Demo)");
            List<String> recipients = new List<String>
            {
                @"Alex Danvy <alex.danvy@microsoft.com>"
            };
            email.AddTo(recipients);
            email.Subject = "Sigfox Demo Alert";
            email.Html = string.Format("<p><b>Sigfox Demo Alert!</b></p><p>The device '{0}' raised an alert.</p>", message.Device);
            email.Text = string.Format("Sigfox Demo Alert\nThe device '{0}' raised an alert.", message.Device);
            await transportWeb.DeliverAsync(email);
        }
    }
}
