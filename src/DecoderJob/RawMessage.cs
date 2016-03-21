using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DecoderJob
{
    public class RawMessage
    {
        public string Device { get; set; }
        [JsonConverter(typeof(HexUInt32Converter))]
        public UInt32 Data { get; set; }
        [JsonConverter(typeof(SecondEpochConverter))]
        public DateTime Time { get; set; }
        public bool Duplicate { get; set; }
        public double Signal { get; set; }
        public string Station { get; set; }
        public double AvgSignal { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Rssi { get; set; }
        public int SeqNumber { get; set; }
    }
}
