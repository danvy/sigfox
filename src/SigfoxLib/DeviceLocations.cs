using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigfoxLib
{
    public class DeviceLocations : List<DeviceLocation>
    {
        public static DeviceLocations Instance = new DeviceLocations();
        public static void Init()
        {
            Instance.Clear();
            var rnd = new Random();
            string id;
            double lat, lon;
            double latrange = 37.784691 - 37.616997;
            double lonrange = -122.499504 - -122.392487;
            for (var i = 1; i <= 10; i++)
            {
                id = string.Format("Device{0:0000000}", i);
                lat = latrange + rnd.NextDouble();
                lon = lonrange + rnd.NextDouble();
                Instance.Add(new DeviceLocation(id, lat, lon));
            }
        }
    }
}
