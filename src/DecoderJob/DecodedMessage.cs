using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoderJob
{
    public class DecodedMessage
    {
        public string Device { get; set; }
        public UInt32 Data { get; set; }
        public Mode Mode { get; set; }
        public Periode Periode { get; set; }
        public FrameType Type { get; set; }
        public double Battery { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public bool ILS { get; set; }
        public double Light { get; set; }
        public Version Version { get; set; }
        public int AlertCount { get; set; }
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
