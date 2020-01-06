using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static lunOptics.libUsbTree.NativeWrapper;

namespace lunOptics.libUsbTree
{
    public class UsbDevice : INotifyPropertyChanged
    {
        #region public properties and methods ---------------------------------

        public string DeviceInstanceID
        {
            get => _deviceInstanceID;
            private set => SetProperty(ref _deviceInstanceID, value);
        }
        public List<string> HardwareIDs
        {
            get => _hardwareIDs;
            private set => SetProperty(ref _hardwareIDs, value);
        }
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }       
        public bool IsConnected { get; internal set; }
        public Guid ClassGuid { get; private set; }
        public string ClassDescription
        {
            get => _classDescription;
            protected set => SetProperty(ref _classDescription, value);
        }
        protected string SnString { get; private set; }
        public int Vid
        {
            get => _vid;
            protected set => SetProperty(ref _vid, value);
        }
        public int Pid
        {
            get => _pid;
            protected set => SetProperty(ref _pid, value);
        }
        public int Rev
        {
            get => _rev;
            protected set => SetProperty(ref _rev, value);
        }
        public int Mi
        {
            get => _mi;
            protected set => SetProperty(ref _mi, value);
        }
        public bool IsInterface
        {
            get => _isUsbInterface;
            protected set => SetProperty(ref _isUsbInterface, value);
        }        
        public ObservableCollection<UsbDevice> children { get; } = new ObservableCollection<UsbDevice>();
        public ObservableCollection<UsbDevice> interfaces { get; } = new ObservableCollection<UsbDevice>();

        public event PropertyChangedEventHandler PropertyChanged;
                
        public virtual bool isEqual(InfoNode other)
        {
            return other != null && this.DeviceInstanceID == other.devInstId;
        }
        public virtual void update(InfoNode info)
        {
            doUpdate(info); // calling virtual functions from constructors is a bad idea => call by indirection. 
        }

        public override string ToString()
        {
            return $"{Description} ({Vid:X4}/{Pid:X4}) #{SnString}";
        }        
        #endregion

        #region construction -------------------------------------------
        private void doUpdate(InfoNode info)
        {
            if (info == null) return;
            if (info.node >= 0) // root node only updates its children
            {
                if (!String.IsNullOrEmpty(info.devInstId)) // driver not yet loaded or other error (happened)
                {
                    DeviceInstanceID = info.devInstId;
                    Vid = info.vid;
                    Pid = info.pid;
                    Mi = info.mi;
                    IsInterface = info.isInterface;
                    SnString = info.serNumStr;

                    Description = cmGetNodePropStrg(info.node, DevPropKeys.Name) ?? "ERR: No Value";
                    ClassGuid = cmGetNodePropGuid(info.node, DevPropKeys.DeviceClassGuid);
                    ClassDescription = cmGetNodePropStrg(info.node, DevPropKeys.DeviceClass) ?? "ERR: No Value";
                    HardwareIDs = cmGetNodePropStringList(info.node, DevPropKeys.HardwareIds);
                    if (HardwareIDs.Count > 0)
                    {
                        Match mRev = Regex.Match(HardwareIDs[0], @"REV[_]?([0-9A-F]{4})", RegexOptions.IgnoreCase);
                        if (mRev.Success) Rev = Convert.ToInt32(mRev.Groups[1].Value, 16);
                    }

                   // var loci = CM_GetNodePropStrg(info.node, DevPropKeys.LocationInfo);
                }
                else
                {
                    DeviceInstanceID = "ERR: No DeviceInstanceID";
                    Description = "ERR: No Description";
                }
            }

            foreach (var childInfo in info.children)
            {
                if (childInfo.isInterface)
                    interfaces.AddIfNew(UsbTree.deviceFactory.MakeOrUpdate(childInfo));
                else
                    children.AddIfNew(UsbTree.deviceFactory.MakeOrUpdate(childInfo));
            }

            foreach (var child in children.ToList())//.Where(c => !info.children.Any(i => c.isEqual(i))).ToList())
            {
                if (!info.children.Any(i => child.isEqual(i)))  // if child is currently connected
                {
                    child.interfaces.Clear();
                    child.children.Clear();
                    children.Remove(child);
                }
            }
        }

        public UsbDevice(InfoNode info = null)
        {
            doUpdate(info);
        }
        
        #endregion

        #region internal fields and methods ----------------------------

        private bool _isUsbInterface;
        private int _vid =-1, _pid=-1, _rev=-1, _mi=-1;        
        private string _deviceInstanceID;
        private string _description;
        private string _classDescription;
        private List<string> _hardwareIDs;
     

        #endregion

        #region INotifyPropertyChanged
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(name);
            }
        }
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    public static partial class MyExtensions
    {
        public static bool AddIfNew<T>(this Collection<T> collection, T val)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (!collection.Contains(val))
            {
                collection.Add(val);
                return true;
            }
            return false;
        }
    }
}
