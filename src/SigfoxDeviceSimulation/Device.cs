using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensitLib;

namespace DeviceSimulator
{
    public class Device
    {
        public string Id { get; set; }
        public double Battery { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
    }
}
