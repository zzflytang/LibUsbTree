using System.Collections.Generic;
using System.Linq;

namespace lunOptics.libUsbTree
{
    public class DeviceFactory
    {
        // override in a derived class
        public virtual UsbDevice newDevice(InfoNode deviceInfo)
        {
            return new UsbDevice(deviceInfo);
        }

        internal UsbDevice MakeOrUpdate(InfoNode deviceInfo)
        {
            var cached = repository.FirstOrDefault(d => d.isEqual(deviceInfo));
            if (cached == null)
            {
                var device = newDevice(deviceInfo);
                repository.Add(device);
                //Debug.WriteLine($"C: {device.Description} - {device.DeviceInstanceID}");
                return device;
            }
            else
            {
                cached.update(deviceInfo);
                //Debug.WriteLine($"U: {cached.Description} - {cached.DeviceInstanceID}");
                return cached;
            }
        }

        private readonly List<UsbDevice> repository = new List<UsbDevice>();
    };
}

