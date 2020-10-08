using System;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;

namespace UXAV.AVnetCore.DeviceSupport
{
    public class SigProviderDevice : IDisposable
    {
        private readonly BasicTriList _basicTriList;

        public SigProviderDevice(BasicTriList basicTriList)
        {
            _basicTriList = basicTriList;
            _basicTriList.SigChange += BasicTriListOnSigChange;
        }

        public SigProviderDevice(SmartObject smartObject)
        {
            SmartObject = smartObject;
            SmartObject.SigChange += SmartObjectOnSigChange;
        }

        public BasicTriList Device
        {
            get
            {
                if (IsSmartObject)
                {
                    return SmartObject.Device as BasicTriList;
                }

                return _basicTriList;
            }
        }

        public SmartObject SmartObject { get; }

        public bool IsSmartObject => SmartObject != null;

        public DeviceBooleanInputCollection BooleanInput =>
            SmartObject != null ? SmartObject.BooleanInput : _basicTriList.BooleanInput;

        public DeviceBooleanOutputCollection BooleanOutput =>
            SmartObject != null ? SmartObject.BooleanOutput : _basicTriList.BooleanOutput;

        public DeviceStringInputCollection StringInput =>
            SmartObject != null ? SmartObject.StringInput : _basicTriList.StringInput;

        public DeviceStringOutputCollection StringOutput =>
            SmartObject != null ? SmartObject.StringOutput : _basicTriList.StringOutput;

        public DeviceUShortInputCollection UShortInput =>
            SmartObject != null ? SmartObject.UShortInput : _basicTriList.UShortInput;

        public DeviceUShortOutputCollection UShortOutput =>
            SmartObject != null ? SmartObject.UShortOutput : _basicTriList.UShortOutput;

        public event SigChangeEventHandler SigChange;

        private void BasicTriListOnSigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            SigChange?.Invoke(this, args);
        }

        private void SmartObjectOnSigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            SigChange?.Invoke(this, args);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (SmartObject != null)
            {
                SmartObject.SigChange -= SmartObjectOnSigChange;
            }
            else
            {
                _basicTriList.SigChange -= BasicTriListOnSigChange;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SigProviderDevice()
        {
            Dispose(false);
        }
    }

    public delegate void SigChangeEventHandler(SigProviderDevice sigProviderDevice, SigEventArgs args);
}