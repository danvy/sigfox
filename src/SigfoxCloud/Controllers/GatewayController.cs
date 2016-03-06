using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IoTSuiteLib;
using Microsoft.AspNet.Mvc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.OptionsModel;
using Newtonsoft.Json;
using SigfoxCloud.Models;

namespace SigfoxCloud.Controllers
{
    [Route("[controller]")]
    public class GatewayController : Controller
    {
        static Dictionary<string, DeviceModel> _whiteList = new Dictionary<string, DeviceModel>();
        static DeviceClient _client = null;
        ServiceSettingsModel _serviceSettings = null;
        static StringBuilder _log = new StringBuilder();
        public GatewayController(IOptions<ServiceSettingsModel> serviceSettings)
        {
            _serviceSettings = serviceSettings?.Value;
        }
        [HttpPost]
        public async void Post([FromBody]PayloadModel payload)
        {
            if (payload == null)
                return;
            payload.device = "Sigfox-" + payload.device;
            AddLog("Device=" + payload.device + " Data=" + payload.data);
            // Temp & humidity payload
            if ((payload.byte1 % 8) == 1)
            {
                await CheckDeviceAsync(payload.device);
                await SendTelemetryAsync(payload);
            }
        }
        private async Task CheckDeviceAsync(string deviceId)
        {
            if (_whiteList.ContainsKey(deviceId))
                return;
            if (_serviceSettings == null)
                return;
            var connectionString = string.Format("HostName={0}.azure-devices.net;SharedAccessKeyName={1};SharedAccessKey={2}",
                            _serviceSettings.Host, _serviceSettings.KeyName, _serviceSettings.Key);
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
        private async Task SendTelemetryAsync(PayloadModel payload)
        {
            if (!_whiteList.ContainsKey(payload.device))
                return;
            var device = _whiteList[payload.device];
            var connectionString = string.Format("HostName={0}.azure-devices.net;DeviceId={1};SharedAccessKey={2}",
                _serviceSettings.Host, device.DeviceId, device.Key);
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
            AddLog("Device=" + payload.device + " Telemetry=" + content);
            await _client.SendEventAsync(new Message(Encoding.UTF8.GetBytes(content)));
        }
        [HttpGet]
        public string Get()
        {
            return _log.ToString();
        }
        private void AddLog(string value)
        {
            if (_log.Length > 4096)
            {
                _log.Remove(4096, _log.Length - 4096);
            }
            _log.AppendLine(string.Format("{0}:{1}", DateTime.UtcNow, value));
        }
        // PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        // DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
