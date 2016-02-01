using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigfoxLib
{
    public class Container
    {
        static Random rnd = new Random();
        private int _simulationVersion = 1;
        private int _simulationVersionNeedPickup = 0;
        private double _fullness;
        private bool _needPick;

        public Container()
        {
        }
        public Container(string id, string deviceId, double fullness, double temp)
        {
            Id = id;
            DeviceId = deviceId;
            Fullness = fullness;
            Temp = temp;
        }
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public DateTime LastUpdate { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool NeedPickup
        {
            get
            {
                return _needPick;
            }
            set
            {
                if (value != _needPick)
                {
                    _needPick = value;
                    if (_needPick)
                        _simulationVersionNeedPickup = _simulationVersion;
                }

            }
        }
        public double Fullness {
            get
            {
                return _fullness;
            }
            set
            {
                _fullness = value > 100 ? 100 : value;
                if (_fullness == 0)
                {
                    NeedPickup = false;
                }
                else if (Fullness > 80)
                {
                    NeedPickup = true;
                }
            }

        }
        public double Temp { get; set; }
        public void Update()
        {
            _simulationVersion += 1;
            LastUpdate = DateTime.Now;
            Fullness += rnd.Next(0, 10);
            if (NeedPickup)
            {
                if (_simulationVersionNeedPickup < (_simulationVersion - 2))
                {
                    Fullness = 0;
                }
            }
        }
    }
}
