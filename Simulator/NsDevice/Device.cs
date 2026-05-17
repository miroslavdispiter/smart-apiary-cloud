using SmartApiary.Simulator.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartApiary.Simulator.NsDevice
{
    public class Device
    {
        private readonly string _deviceUuid;
        private string? _accessToken = null;
        private string? _serialNumber = null;
        private readonly Random _rnd = new Random();
        public int Battery { get; private set; }

        public Device()
        {
            _deviceUuid = Guid.NewGuid().ToString();
            Battery = 100;
        }

        public Measurement GenerateSample()
        {
            double baseWeight = 30.0 + _rnd.NextDouble() * 20;
            baseWeight += (_rnd.NextDouble() - 0.48) * 0.5;
            baseWeight = Math.Max(5.0, baseWeight);

            Measurement measurement = new Measurement()
            {
                WeightKg = Math.Round(baseWeight, 2),
                Humidity = Math.Round(40.0 + _rnd.NextDouble() * 40, 1),
                Temperature = Math.Round(32.0 + _rnd.NextDouble() * 8, 1),
            };

            ConsumeBattery();
            return measurement;
        }

        private void ConsumeBattery()
        {
            int consumedAmount = _rnd.Next(3, 7);
            Battery = Battery - consumedAmount;
            if (Battery < 0) Battery = 0;
        }

        public void SetAccessToken(string? accessToken)
        {
            if(_accessToken is not null)
            {
                Console.WriteLine("Access token is already set");
                throw new InvalidOperationException();
            }
            _accessToken = accessToken;
        }
        public void SetSerialNumber(string serialNumber)
        {
            if (_serialNumber is not null)
            {
                Console.WriteLine("Serial Number is already set");
                throw new InvalidOperationException();
            }
            _serialNumber = serialNumber;
        }

        public string DeviceUuid 
        { 
            get { return _deviceUuid; }
        }
        public string? AccessToken
        {
            get { return _accessToken; }
        }
        public string? SerialNumber
        {
            get { return _serialNumber; }
        }
    }
}
