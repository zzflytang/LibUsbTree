using lunOptics.libUsbTree;
using System;
using System.Collections.Specialized;
using System.Threading;
using static System.Console;

namespace UsbTreeTester
{
    class Program
    {
        static void Main()
        {
            var usbTree = new UsbTree();

            ForegroundColor = ConsoleColor.Blue;       
            WriteLine("===============================================================");
            WriteLine(" Currently connected USB devices");
            WriteLine("===============================================================\n");
                        

            WriteLine("FLAT DEVICE LIST: ---------------------------------------------");
            ForegroundColor = ConsoleColor.White;

            foreach (var device in usbTree.DeviceList)
            {
                WriteLine($"  - {device}");
            }

            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\nHIERARCHICAL DEVICE LIST: -----------------------------------");
            ForegroundColor = ConsoleColor.White;

            foreach (var root in usbTree.DeviceTree.children) 
            {
                PrintRecursively(root);          
            }

            ForegroundColor = ConsoleColor.Blue;
            WriteLine("\n\nPlug in/out devices to see changes or press any key to stop\n");
            usbTree.DeviceList.CollectionChanged += DevicesChanged;

            while (!KeyAvailable) Thread.Sleep(100);

            // cleanup            
            usbTree.Dispose();
        }

        private static void DevicesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ForegroundColor = ConsoleColor.DarkGreen;
                    foreach (UsbDevice device in e.NewItems)
                    {
                        WriteLine($"+ {device}");                        
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    ForegroundColor = ConsoleColor.DarkGray;
                    foreach (UsbDevice device in e.OldItems)
                    {
                        WriteLine($"- {device.ToString()}");                        
                    }
                    break;

                default:
                    WriteLine("Unexpected Change Event");
                    break;
            }
        }

        private static void PrintRecursively(UsbDevice device, int level = 2)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            Write(new String(' ', level ));  // indent proprtional to level
            WriteLine(device.ToString());
            foreach (var c in device.children)
            {
                PrintRecursively(c, level + 2);
            }
        }
    }
}
