using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SigfoxCloud.Models
{
    public class DeviceModel
    {
        public string DeviceId { get; set; }
        public string DeviceRegId { get; internal set; }
        public string Key { get; set; }
    }
}
