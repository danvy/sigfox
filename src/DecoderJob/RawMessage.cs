using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoderJob
{
    public class RawMessage
    {
        public string Device { get; set; }
        public string Data { get; set; }
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
