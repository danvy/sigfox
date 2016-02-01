using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoTSuiteLib;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using SigfoxCloud.Models;

namespace Test
{
    class Program
    {
        static Dictionary<string, DeviceModel> _whiteList = new Dictionary<string, DeviceModel>();
        static DeviceClient _client = null;

        static void Main(string[] args)
        {
            var payload = new PayloadModel();
            payload.device = "Sigfox-FAE83";
            payload.byte1 = 137;
            payload.byte2 = 93;
            payload.byte3 = 48;
            payload.byte4 = 95;
            // Temp & humidity payload
            if ((payload.byte1 % 8) == 1)
            {
                CheckDeviceAsync(payload.device).Wait();
                SendTelemetryAsync(payload).Wait();
            }
            return;

            #region Work in progress
            var payloads = new uint[] { 0x895d305f, 0xa95d3e18, 0xaa5d3c18, 0xab5d3818, 0xac5d3418, 0xa96d3318 };
            // ?
            //vert
            //a95d3e18 mode: 169 temperatureMSB: 93 temperatureLSB: 62
            //humidity: 24 temp: 6206 temp2: 15896
            //jaune
            //aa5d3c18 mode: 170 temperatureMSB: 93 temperatureLSB: 60
            //humidity: 24 temp: 6204 temp2: 15384
            //bleu
            //ab5d3818 mode: 171 temperatureMSB: 93 temperatureLSB: 56
            //humidity: 24 temp: 6200 temp2: 14360
            //bleu foncé
            //ac5d3418 mode: 172 temperatureMSB: 93 temperatureLSB: 52
            //humidity: 24 temp: 6196 temp2: 13336
            foreach (var p in payloads)
            {
                var bytes = BitConverter.GetBytes(p);
                Array.Reverse(bytes);
                if ((bytes[0] % 8) == 1)
                {
                    //MSB : Les 4 premiers bits de l'octet temperatureMSB
                    var MSB = Math.Floor(bytes[1] / Math.Pow(2, 4));
                    var LSB = bytes[2] % 64;
                    //On concatene les deux : MSB+LSB, on retranche 200 puis on divise par 8
                    //On obtient la temperature en °C
                    var temperature = (((MSB * Math.Pow(2, 6)) + LSB) - 200) / 8;
                    var humidity = bytes[3] * 0.5;
                    Console.WriteLine(string.Format("Payload={0} temperature={1} humidity={2}", p, temperature, humidity));
                }
                //var mode = (p1 << 29) >> 29;
                //var timeframe = (p1 << 27) >> 30;
                //var type = (p1 << 25) >> 30;
                //var battery = (((p1 << 24) >> 27)) * 0.05 * 2.7;
                //var temp1 = 0; // ((Convert.ToInt16(new byte[] { (byte)(bytes[1] % 64), (byte)((bytes[2] << 2) >> 6)})));
                ////temp1 = 5.0 / 9.0 * (temp1 - 32);
                //double hum = 0;
                //double temp = 0;
                //switch (type)
                //{
                //    case 0: //Button
                //        break;
                //    case 1: //Temp-Humidity
                //        hum = ((p1 << 0) >> 24) * 2;
                //        temp = (((p1 << 20) >> 28) * 0.01) + 20;
                //        temp = ((p1 << 12) >> 28) << 5;
                //        temp += ((p << 20) >> 28);
                //        temp = (temp - 200) / 8;
                //        break;
                //    case 2: //Light
                //        break;
                //    case 3: //Door
                //        break;
                //    case 4: //Move
                //        break;
                //    case 5: //Reed switch
                //        break;
                //    default:
                //        break;
                //}
                //Console.WriteLine(string.Format("mode={0} time={1} type={2} battery={3} temp1={4} temp={5} hum={6}", mode, timeframe, type, battery, temp1, temp, hum));
                //Console.WriteLine();
            }
            #endregion
            Console.Read();
        }
        private static async Task CheckDeviceAsync(string deviceId)
        {
            if (_whiteList.ContainsKey(deviceId))
                return;
            //var connectionString = IotHubConnectionStringBuilder.Create("sigfoxmonitoring",
            //    AuthenticationMethodFactory.CreateAuthenticationWithSharedAccessPolicyKey(deviceId, "registryReadWrite", "weAJyh51pxOk5f/OVZRGXc4JR6AJDg+JYjiK3rlDTzs="));
            var connectionString = string.Format("HostName={0}.azure-devices.net;SharedAccessKeyName={1};SharedAccessKey={2}",
                            "sigfoxmonitoring", "iothubowner", "B2LsypvIEz7bdy0217QYfeUvO1xUjKVujlte4wETrvM=");
            var registry = Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(connectionString);
            var deviceReg = await registry.GetDeviceAsync(deviceId);
            if (deviceReg == null)
            {
                deviceReg = new Microsoft.Azure.Devices.Device(deviceId);
                deviceReg = await registry.AddDeviceAsync(deviceReg);
            }
            var device = new DeviceModel();
            device.DeviceId = deviceId;
            //device.DeviceRegId = deviceReg.Id;
            device.Key = deviceReg.Authentication.SymmetricKey.PrimaryKey;
            _whiteList.Add(deviceId, device);
            var data = new DeviceMetaData();
            data.Version = "1.0";
            data.IsSimulatedDevice = false;
            data.Properties.DeviceID = deviceId;
            data.Properties.FirmwareVersion = "42";
            data.Properties.HubEnabledState = true;
            data.Properties.Processor = "Foo";
            data.Properties.Platform = "Yep";
            data.Properties.SerialNumber = "Sigfox-" + deviceId;
            data.Properties.InstalledRAM = "1 MB";
            data.Properties.ModelNumber = "007-BOND";
            data.Properties.Manufacturer = "Sigfox";
            //data.Properties.UpdatedTime = DateTime.UtcNow;
            data.Properties.DeviceState = DeviceState.Normal;
            var content = JsonConvert.SerializeObject(data);
            connectionString = string.Format("HostName={0}.azure-devices.net;DeviceId={1};SharedAccessKey={2}",
                "sigfoxmonitoring", device.DeviceId, device.Key);
            _client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
            await _client.SendEventAsync(new Message(Encoding.UTF8.GetBytes(content)));
        }
        private static async Task SendTelemetryAsync(PayloadModel payload)
        {
            if (!_whiteList.ContainsKey(payload.device))
                return;
            var device = _whiteList[payload.device];
            var connectionString = string.Format("HostName={0}.azure-devices.net;DeviceId={1};SharedAccessKey={2}",
                "sigfoxmonitoring", device.DeviceId, device.Key);
            _client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
            var MSB = Math.Floor(payload.byte2 / Math.Pow(2, 4));
            var LSB = payload.byte3 % 64;
            var temperature = (((MSB * Math.Pow(2, 6)) + LSB) - 200) / 8;
            var humidity = payload.byte4 * 0.5;
            var data = new DeviceMonitoringData();
            data.DeviceId = payload.device;
            data.Temperature = temperature;
            data.Humidity = humidity;
            var content = JsonConvert.SerializeObject(data);
            await _client.SendEventAsync(new Message(Encoding.UTF8.GetBytes(content)));
        }
    }
}
