using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Danvy.Azure;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SensitLib;
using StackExchange.Redis;

namespace AlertJob
{
    public class EventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;
        EventHubClient hubClient;
        CloudStorageAccount storageAccount;
        private CloudBlobContainer blobContainer;
        private SqlConnection sqlConnection;
        private SqlCommand sqlCommand;
        private ConnectionMultiplexer cacheConnection;
        private IDatabase cacheDatabase;

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            if (sqlConnection != null)
                sqlConnection.Close();
            if (!WebJobsHelper.RunAsWebJobs)
                Console.WriteLine(string.Format("Processor Shuting Down. Partition '{0}', Reason: '{1}'.",
                    partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
                await context.CheckpointAsync();
        }
        public async Task OpenAsync(PartitionContext context)
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
                    hubClient = EventHubClient.CreateFromConnectionString(
                        ConfigurationManager.ConnectionStrings["SigfoxDemoAlertSender"].ConnectionString,
                        "alert");
                    cacheConnection = await ConnectionMultiplexer.ConnectAsync(CloudConfigurationManager.GetSetting("CacheConnectionString"));
                    cacheDatabase = cacheConnection.GetDatabase();
                    sqlConnection = new SqlConnection(CloudConfigurationManager.GetSetting("SqlConnectionString"));
                    sqlConnection.Open();
                    //sqlCommand = new SqlCommand("InsertAlert", sqlConnection) { CommandType = CommandType.StoredProcedure };
                    //sqlCommand.Parameters.Add(new SqlParameter("@Device", SqlDbType.VarChar));
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
        }
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            if (blobContainer == null)
                return;
            try
            {
                foreach (var message in messages)
                {
                    var jsonMessage = Encoding.UTF8.GetString(message.GetBytes());
                    if (!WebJobsHelper.RunAsWebJobs)
                        Console.WriteLine(string.Format("Message: {0}", jsonMessage));
                    DecodedMessage messageObj = null;
                    try
                    {
                        messageObj = JsonConvert.DeserializeObject<DecodedMessage>(jsonMessage);
                    }
                    catch
                    {

                    }
                    if (messageObj != null)
                    {
                        await CheckAlertAsync(messageObj);
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
        private async Task CheckAlertAsync(DecodedMessage message)
        {
            if (cacheDatabase == null)
                return;
            string json = await cacheDatabase.StringGetAsync(message.Device);
            if (json == null)
                return;
            DecodedMessage cached = null;
            try
            {
                cached = JsonConvert.DeserializeObject<DecodedMessage>(json);
            }
            catch
            {

            }
            if (cached == null)
                return;
            if (Math.Abs(message.Temperature - cached.Temperature) > 10)
            {
                //var alertMessage = new AlertMessage();
                //hubClient.Send(new EventData(Encoding.UTF8.GetBytes(alertMessage)));
            }
        }
    }
}
