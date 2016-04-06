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

namespace DecoderJob
{
    public class EventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;
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
                var s = string.Empty;
                try
                {
                    retries--;
                    s = "storage";
                    storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["SigfoxDemoStorage"].ConnectionString);
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    blobContainer = blobClient.GetContainerReference("device");
                    blobContainer.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Off });
                    blobContainer.CreateIfNotExists();
                    s = "cache";
                    cacheConnection = await ConnectionMultiplexer.ConnectAsync(ConfigurationManager.ConnectionStrings["SigfoxDemoCache"].ConnectionString);
                    cacheDatabase = cacheConnection.GetDatabase();
                    s = "database";
                    sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["SigfoxDemoDatabase"].ConnectionString);
                    sqlConnection.Open();
                    sqlCommand = new SqlCommand("InsertMessage", sqlConnection) { CommandType = CommandType.StoredProcedure };
                    sqlCommand.Parameters.Add(new SqlParameter("@Device", SqlDbType.VarChar));
                    sqlCommand.Parameters.Add(new SqlParameter("@Data", SqlDbType.VarChar));
                    sqlCommand.Parameters.Add(new SqlParameter("@Mode", SqlDbType.TinyInt));
                    sqlCommand.Parameters.Add(new SqlParameter("@Periode", SqlDbType.TinyInt));
                    sqlCommand.Parameters.Add(new SqlParameter("@FrameType", SqlDbType.TinyInt));
                    sqlCommand.Parameters.Add(new SqlParameter("@Battery", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Temperature", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Humidity", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@ILS", SqlDbType.Bit));
                    sqlCommand.Parameters.Add(new SqlParameter("@Light", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Version", SqlDbType.VarChar));
                    sqlCommand.Parameters.Add(new SqlParameter("@AlertCount", SqlDbType.Int));
                    sqlCommand.Parameters.Add(new SqlParameter("@TimeStamp", SqlDbType.DateTime));
                    sqlCommand.Parameters.Add(new SqlParameter("@Duplicate", SqlDbType.Bit));
                    sqlCommand.Parameters.Add(new SqlParameter("@Signal", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Station", SqlDbType.VarChar));
                    sqlCommand.Parameters.Add(new SqlParameter("@AvgSignal", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Latitude", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Longitude", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@Rssi", SqlDbType.Float));
                    sqlCommand.Parameters.Add(new SqlParameter("@SeqNumber", SqlDbType.Int));
                    retries = 0;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error opening destination Event Hub: " + e.Message + "(" + s + ")");
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
                        await StoreInBlobAsync(messageObj.Device, jsonMessage);
                        await StoreInDatabaseAsync(messageObj);
                        await StoreInCacheAsync(messageObj.Device, jsonMessage);
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

        private async Task StoreInCacheAsync(string device, string message)
        {
            if (cacheDatabase == null)
                return;
            await cacheDatabase.StringSetAsync("message-" + device, message);
        }

        private async Task StoreInDatabaseAsync(DecodedMessage message)
        {
            if (sqlCommand == null)
                return;
            sqlCommand.Parameters["@Device"].Value = message.Device;
            sqlCommand.Parameters["@Data"].Value = message.Data;
            sqlCommand.Parameters["@Mode"].Value = message.Mode;
            sqlCommand.Parameters["@Periode"].Value = message.Periode;
            sqlCommand.Parameters["@FrameType"].Value = message.Type;
            sqlCommand.Parameters["@Battery"].Value = message.Battery;
            sqlCommand.Parameters["@Temperature"].Value = message.Temperature;
            sqlCommand.Parameters["@Humidity"].Value = message.Humidity;
            sqlCommand.Parameters["@ILS"].Value = message.ILS;
            sqlCommand.Parameters["@Light"].Value = message.Light;
            sqlCommand.Parameters["@Version"].Value = message.Version.ToString();
            sqlCommand.Parameters["@AlertCount"].Value = message.AlertCount;
            sqlCommand.Parameters["@TimeStamp"].Value = message.Time;
            sqlCommand.Parameters["@Duplicate"].Value = message.Duplicate;
            sqlCommand.Parameters["@Signal"].Value = message.Signal;
            sqlCommand.Parameters["@Station"].Value = message.Station;
            sqlCommand.Parameters["@AvgSignal"].Value = message.AvgSignal;
            sqlCommand.Parameters["@Latitude"].Value = message.Latitude;
            sqlCommand.Parameters["@Longitude"].Value = message.Longitude;
            sqlCommand.Parameters["@Rssi"].Value = message.Rssi;
            sqlCommand.Parameters["@SeqNumber"].Value = message.SeqNumber;
            await sqlCommand.ExecuteNonQueryAsync();
        }

        private async Task StoreInBlobAsync(string device, string message)
        {
            if (blobContainer == null)
                return;
            var latestBlob = blobContainer.GetBlockBlobReference("Latest/" + device + ".json");
            await latestBlob.UploadTextAsync(message);
        }
    }
}
