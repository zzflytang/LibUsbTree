using lunOptics.libUsbTree;
using System;

namespace Hello_UsbTree
{
    class Program
    {
        static void Main(string[] args)
        {
            Guid hub = new Guid("36fc9e60-c465-11cf-8056-444553540000");
            using (var usbTree = new UsbTree())
            {
                foreach (var device in usbTree.DeviceList)
                {
                    if (((lunOptics.libUsbTree.UsbDevice)device).ClassDescription.Equals("USB") &&
                        !((lunOptics.libUsbTree.UsbDevice)device).ClassGuid.Equals(hub) && true)
                    {
                        Console.WriteLine(device);
                        foreach (var fun in device.functions)
                        {
                            String classDescription = ((lunOptics.libUsbTree.UsbDevice)fun).ClassDescription;
                            if (classDescription.Equals("Ports"))
                                Console.WriteLine("\t" + classDescription + " " + fun.Description + " [MI_" + fun.Mi +
                                                  "]");
                        }
                    }
                }
            }

            Console.WriteLine("\n\nAny key to exit");
            while (!Console.KeyAvailable)
                ;
        }
    }
}
