using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigfoxEventSimulator
{
    public class DeviceData
    {
        public string Device { get; set; }
        public string Data { get; set; }
        public int Temp { get; set; }
        public int Voltage { get; set; }
        public DateTime Time { get; set; }
        public bool Duplicate { get; set; }
        public string SNR { get; set; }
        public string Station { get; set; }
        public float AvgSignal { get; set; }
        public int Lat { get; set; }
        public int Lng { get; set; }
        public float Rssi { get; set; }
        public int SeqNumber { get; set; }
    }
}
