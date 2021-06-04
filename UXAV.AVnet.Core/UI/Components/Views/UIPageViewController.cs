using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Components.Views
{
    public abstract class UIPageViewController : UIViewControllerBase
    {
        private readonly Core3ControllerBase _core3Controller;
        private readonly Mutex _mutex = new Mutex();

        /// <summary>
        /// Constructor for a Page based view controller
        /// </summary>
        protected UIPageViewController(Core3ControllerBase core3Controller, uint pageNumber)
            : base(core3Controller, pageNumber)
        {
            _core3Controller = core3Controller;
            if (core3Controller.Pages.Any(p => p.PageNumber == pageNumber))
                throw new Exception(
                    $"Cannot add page controller with digital join {pageNumber}, page already exists");

            _core3Controller.Pages.Add(this);
            SigProvider.SigChange += DeviceOnSigChange;
        }

        public new Core3ControllerBase Core3Controller => _core3Controller;

        /// <summary>
        /// All other pages for this UI not including this one
        /// </summary>
        public IEnumerable<UIPageViewController> OtherPages
        {
            get { return _core3Controller.Pages.Where(p => p != this); }
        }

        /// <summary>
        /// The number of the page join
        /// </summary>
        public uint PageNumber => VisibleJoinNumber;

        /// <summary>
        /// True if currently visible
        /// </summary>
        public override bool Visible
        {
            get => SigProvider.BooleanInput[VisibleJoinNumber].BoolValue;
            protected set
            {
                _mutex.WaitOne();
                if (SigProvider.BooleanInput[VisibleJoinNumber].BoolValue == value)
                {
                    _mutex.ReleaseMutex();
                    return;
                }
                Logger.Debug($"Page {VisibleJoinNumber} Visible set to {value}");

                RequestedVisibleState = value;

                if (value)
                {
                    if (_core3Controller.Pages.PreviousPages.Contains(this))
                    {
                        _core3Controller.Pages.PreviousPages.Remove(this);
                    }

                    foreach (var page in OtherPages.Where(page => page.Visible))
                    {
                        page.Visible = false;
                        _core3Controller.Pages.PreviousPages.Add(page);
                    }

                    Logger.Debug($"Previous Pages Count = {_core3Controller.Pages.PreviousPages.Count}");
                }

                OnVisibilityChanged(this,
                    new VisibilityChangeEventArgs(value, value
                        ? VisibilityChangeEventType.WillShow
                        : VisibilityChangeEventType.WillHide));

                SigProvider.BooleanInput[VisibleJoinNumber].BoolValue = value;

                Logger.Debug("Page Join {0} set to {1}", VisibleJoinNumber,
                    SigProvider.BooleanInput[VisibleJoinNumber].BoolValue);

                OnVisibilityChanged(this,
                    new VisibilityChangeEventArgs(value, value
                        ? VisibilityChangeEventType.DidShow
                        : VisibilityChangeEventType.DidHide));

                _mutex.ReleaseMutex();
            }
        }

        public UIPageViewController PreviousPage => _core3Controller.Pages.PreviousPages.LastOrDefault();

        public bool CanGoBack => PreviousPage != null;

        private void DeviceOnSigChange(SigProviderDevice currentDevice, SigEventArgs args)
        {
            if (args.Event != eSigEvent.BoolChange || args.Sig.Number != VisibleJoinNumber || !args.Sig.BoolValue)
                return;

            Logger.Debug("Page Feedback {0} = {1}", args.Sig.Number, args.Sig.BoolValue);
        }

        public void Back()
        {
            PreviousPage?.Show();
        }

        protected override void OnTimedOut(ActivityTimeOut timeout, ActivityTimedOutEventArgs args)
        {
            Back();
        }
    }
}