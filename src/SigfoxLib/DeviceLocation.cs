using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigfoxLib
{
    public class DeviceLocation
    {
        public DeviceLocation()
        {
        }
        public DeviceLocation(string deviceId, double latitude, double longitude)
        {
            DeviceId = deviceId;
            Latitude = latitude;
            Longitude = longitude;
        }
        public string DeviceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
