using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Danvy.Azure;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using SensitLib;

namespace DecoderJob
{
    public class EventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;
        EventHubClient hubClient;
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
                    hubClient = EventHubClient.CreateFromConnectionString(
                        ConfigurationManager.ConnectionStrings["SigfoxDemoDispatchSender"].ConnectionString,
                        "dispatch");
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
            return Task.FromResult<object>(null);
        }
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            if (hubClient == null)
                return;
            try
            {
                foreach (var message in messages)
                {
                    var rawMessage = Encoding.UTF8.GetString(message.GetBytes());
                    if (!WebJobsHelper.RunAsWebJobs)
                        Console.WriteLine(string.Format("EventData Source: {0}", rawMessage));
                    RawMessage raw = null;
                    try
                    {
                        raw = JsonConvert.DeserializeObject<RawMessage>(rawMessage);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Error deserializing a message:\n" + e.Message + "\n" + rawMessage);
                    }
                    var decoded = DecodeMessage(raw);
                    var decodedMessage = JsonConvert.SerializeObject(decoded);
                    //if (!WebJobsHelper.RunAsWebJobs)
                        Console.Out.WriteLine(string.Format("EventData Destination: {0}", decodedMessage));
                    hubClient.Send(new EventData(Encoding.UTF8.GetBytes(decodedMessage)));
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

        private DecodedMessage DecodeMessage(RawMessage raw)
        {
            var decoded = new DecodedMessage();
            decoded.AvgSignal = raw.AvgSignal;
            decoded.Data = raw.Data;
            decoded.Device = raw.Device;
            decoded.Duplicate = raw.Duplicate;
            decoded.Latitude = raw.Latitude;
            decoded.Longitude = raw.Longitude;
            decoded.Rssi = raw.Rssi;
            decoded.SeqNumber = raw.SeqNumber;
            decoded.Signal = raw.Signal;
            decoded.Station = raw.Station;
            decoded.Time = raw.Time;
            var d = StringToByteArray(raw.Data);
            decoded.Mode = (Mode)(d[0] & 0x7);
            decoded.Periode = (Periode)((d[0] & 0x18) >> 3);
            decoded.Type = (FrameType)((d[0] >> 4) & 0x3);
            decoded.Battery = (((d[0] >> 7) * 16) + (d[1] >> 4) + 54) / 20.0;
            decoded.Temperature = ((d[1] & 0xF) * 64 - 200) / 10.0;
            if ((decoded.Type == FrameType.Regular) && (decoded.Mode == Mode.Light))
            {
                var valeur = (d[2] << 2) >> 2;
                var multi = d[2] >> 6;
                switch (multi)
                {
                    case 0:
                        multi = 1;
                        break;
                    case 1:
                        multi = 8;
                        break;
                    case 2:
                        multi = 64;
                        break;
                    case 3:
                        multi = 2014;
                        break;
                }
                decoded.Light = Convert.ToSingle(multi * valeur * 0.01);
            }
            //Door
            else if ((decoded.Type == FrameType.Regular) && (decoded.Mode == Mode.Door))
            {
                //
            }
            else
            {
                decoded.Temperature = (((d[1] >> 4) << 6) + (d[2] & 0x3F) - 200) / 8.0;
                decoded.ILS = (d[2] & 0x20) != 0;
            }
            //Button
            if (decoded.Mode == Mode.Button)
            {
                decoded.Version = new Version(string.Format("{0}.{1}", d[3] >> 4, d[3] & 0xF));
            }
            //Temperature
            else if (decoded.Mode == Mode.TemperatureHumidity)
            {
                decoded.Humidity = d[3] * 0.5;
            }
            else
            {
                decoded.AlertCount = Convert.ToInt32(d[3]);
            }
            return decoded;
        }
        public byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
