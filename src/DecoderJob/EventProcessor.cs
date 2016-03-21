using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace DecoderJob
{
    public class EventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;
        EventHubClient hubClient;
        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(string.Format("Processor Shuting Down. Partition '{0}', Reason: '{1}'.",
                partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
                await context.CheckpointAsync();
        }
        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine(string.Format("EventProcessor initialization. Partition: '{0}', Offset: '{1}'",
                context.Lease.PartitionId, context.Lease.Offset));
            partitionContext = context;
            hubClient = EventHubClient.CreateFromConnectionString(ConfigurationManager.ConnectionStrings["DestinationBusConnectionString"].ConnectionString,
                ConfigurationManager.AppSettings["DestinationEventHubName"]);
            checkpointStopWatch = new Stopwatch();
            checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }
        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    var rawMessage = Encoding.UTF8.GetString(message.GetBytes());
                    Console.WriteLine(string.Format("EventData Source: {0}", rawMessage));
                    var raw = JsonConvert.DeserializeObject<RawMessage>(rawMessage);
                    var decoded = DecodeMessage(raw);
                    var decodedMessage = JsonConvert.SerializeObject(decoded);
                    Console.WriteLine(string.Format("EventData Destination: {0}", decodedMessage));
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
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in processing: " + exp.Message);
                Console.ResetColor();
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
            var d = BitConverter.GetBytes(raw.Data).Reverse().ToArray();
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
    }
}
