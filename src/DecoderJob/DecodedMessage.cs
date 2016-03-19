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
        public string Data { get; set; }
        public Mode Mode { get; set; }
        public Periode Periode { get; set; }
        public FrameType Type { get; set; }
        public float Battery { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public bool ILS { get; set; }
        public float Light { get; set; }
        public string Version { get; set; }
        public int AlertCount { get; set; }
        public string Time { get; set; }
        public string Duplicate { get; set; }
        public int Signal { get; set; }
        public string Station { get; set; }
        public int AvgSignal { get; set; }
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public int Rssi { get; set; }
        public int SeqNumber { get; set; }
    }
}
