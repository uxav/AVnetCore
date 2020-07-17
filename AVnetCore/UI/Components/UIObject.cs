using System;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components
{
    public abstract class UIObject : IDisposable, ISigProvider
    {
        protected UIObject(ISigProvider sigProvider)
        {
            SigProvider = sigProvider.SigProvider;
        }

        protected UIObject(SmartObject smartObject)
        {
            SigProvider = new SigProviderDevice(smartObject);
        }

        public SigProviderDevice SigProvider { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SigProvider?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UIObject()
        {
            Dispose(false);
        }
    }
}