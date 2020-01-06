//using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using static lunOptics.libUsbTree.NativeWrapper;


namespace lunOptics.libUsbTree
{
    public class UsbTree : IDisposable  
    {
        #region properties -----------------------------------------------------------------------------

        public UsbDevice DeviceTree { get; } = new UsbDevice();
        public ObservableCollection<UsbDevice> DeviceList { get; } = new ObservableCollection<UsbDevice>();
       
        public static DeviceFactory deviceFactory { get; private set; }
        #endregion

        #region construction/deconstruction ------------------------------------------------------------
               
        public UsbTree(SynchronizationContext SyncContext = null, DeviceFactory factory = null)
        {
            deviceFactory = factory ?? new DeviceFactory();  // for simple use do not require (but allow) dependency injection
            rootNodes = FindUsbRoots(); 

            timer.Interval = 200;
            timer.Elapsed += (s, e) =>
            {
                if (SyncContext == null) CheckForChanges();
                else SyncContext.Post(state => CheckForChanges(), null);
            };
            CheckForChanges();  // implicitly starts timer            
        }
               
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region private fields and methods -------------------------------------------------------------

        private InfoNode oldTree, newTree;        

        private void CheckForChanges()
        {
            timer.Stop(); // avoid reentrance

            var sw = new Stopwatch();
            sw.Start();
            newTree = new InfoNode(rootNodes);
            if(!newTree.isEqual(oldTree))
            {
                newTree.readDetails();
                DeviceTree.update(newTree);  // update the complete device tree. Add/remove devices if necessary
                UpdateDeviceList();          // reflect all changes in the flat device list     
                oldTree = newTree;
            }
            sw.Stop();
            Debug.WriteLine(sw.Elapsed.TotalMilliseconds);

            timer.Start();
        }
             

        void UpdateDeviceList()
        {
            var flatList = DeviceTree.children.myFlatten(i => i.children);
            var newDevices = flatList.Except(DeviceList).ToList();
            var removedDevices = DeviceList.Except(flatList).ToList();
            
            newDevices.ForEach(d => DeviceList.Add(d));
            removedDevices.ForEach(d => DeviceList.Remove(d));           
        }

        private List<int> FindUsbRoots()
        {
            var roots = new HashSet<int>();
            foreach (var devInstID in cmGetDevInstIDs("USB")) // loop through all USB device interface ids
            {
                int node = cmLocateNode(devInstID);
                int parent;
                while ((parent = cmGetParentNode(node)) != -1)
                {
                    if (!cmGetDevInstIdFromNode(parent).StartsWith("USB")) // bubble up until parent is not a USB device
                    {
                        roots.Add(node);  // hash set will reject multiple addition of same node
                        break;
                    }
                    node = parent;
                }
            }
            return roots.ToList();
        }

        private readonly List<int> rootNodes;       

        private System.Timers.Timer timer = new System.Timers.Timer();

        #endregion
    }

    public partial class MyExtensions
    {
        public static IEnumerable<T> myFlatten<T>(this IEnumerable<T> e, Func<T, IEnumerable<T>> f)
        {
            return e.SelectMany(c => f(c).myFlatten(f)).Concat(e);
        }
    }
}

