using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensitLib;

namespace DeviceSimulator
{
    public class Devices : List<Device>
    {
        public static Devices Instance = new Devices();
        public static void Init(int count = 100)
        {
            Instance.Clear();
            var rnd = new Random();
            string id;
            double lat, lon;
            double latrange = 37.784691 - 37.616997;
            double lonrange = -122.499504 - -122.392487;
            for (var i = 1; i <= count; i++)
            {
                id = string.Format("DS{0:00000}", i);
                lat = latrange + rnd.NextDouble();
                lon = lonrange + rnd.NextDouble();
                Instance.Add(new Device() { Id = id, Battery = 100, Humidity = 50, Temperature = 20 });
            }
        }
    }
}
