using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SigfoxCloud.Models
{
    public class PayloadModel
    {
        public string device { get; set; }
        public string data { get; set; }
        public byte byte1 { get; set; }
        public byte byte2 { get; set; }
        public byte byte3 { get; set; }
        public byte byte4 { get; set; }
        public string time { get; set; }
        public string duplicate { get; set; }
        public string snr { get; set; }
        public string station { get; set; }
        public string avgSignal { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string rssi { get; set; }
        public string seqNumber { get; set; }
    }
}
