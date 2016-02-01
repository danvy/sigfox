using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigfoxLib
{
    public class Containers : List<Container>
    {
        public static Containers Instance = new Containers();
        public static void Init()
        {
            Instance.Clear();
            var rnd = new Random();
            double latstart = 37.616997;
            //double lonstart = -122.499504 - -122.392487;
            double lonstart = -122.392487;
            double latrange = 37.784691 - latstart;
            double lonrange = -122.499504 - lonstart;
            Container container;
            for (var i = 1; i <= 100; i++)
            {
                container = new Container();
                container.Id = string.Format("Container{0:0000000}", i);
                container.DeviceId = string.Format("Device{0:0000000}", i);
                container.Fullness = rnd.Next(0, 100);
                container.Temp = rnd.Next(12, 25);
                container.Latitude = latstart + (latrange * rnd.NextDouble());
                container.Longitude = lonstart + (lonrange * rnd.NextDouble());
                Instance.Add(container);
            }
        }
        public void Update()
        {
            foreach (var device in Instance)
            {
                device.Update();
            }
        }
    }
}
