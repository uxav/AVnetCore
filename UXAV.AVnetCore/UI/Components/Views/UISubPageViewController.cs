using Crestron.SimplSharpPro;
using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components.Views
{
    public abstract class UISubPageViewController : UIViewControllerBase
    {
        private bool _listeningToSigChanges;

        protected UISubPageViewController(ISigProvider sigProvider, uint visibleJoinNumber, uint animationCompleteJoinNumber = 0)
            : base(sigProvider, visibleJoinNumber)
        {
            if(animationCompleteJoinNumber > 0) {
                AnimationCompleteFeedbackJoin = sigProvider.SigProvider.BooleanOutput[animationCompleteJoinNumber];
            }
        }

        protected UISubPageViewController(UITabControllerBase tabController, uint tabButtonNumber, uint visibleJoinNumber, uint animationCompleteJoinNumber = 0)
            : base(tabController, visibleJoinNumber)
        {
            if (animationCompleteJoinNumber > 0)
            {
                AnimationCompleteFeedbackJoin = tabController.SigProvider.BooleanOutput[animationCompleteJoinNumber];
            }

            tabController.AddView(tabButtonNumber, this);
        }

        public BoolOutputSig AnimationCompleteFeedbackJoin { get; }

        public override bool Visible
        {
            get => base.Visible;
            protected set
            {
                if (value && !_listeningToSigChanges && AnimationCompleteFeedbackJoin != null)
                {
                    _listeningToSigChanges = true;
                    SigProvider.SigChange += SigProviderOnSigChange;
                }
                base.Visible = value;
            }
        }

        private void SigProviderOnSigChange(SigProviderDevice sigproviderdevice, SigEventArgs args)
        {
            if(args.Sig != AnimationCompleteFeedbackJoin || args.Event != eSigEvent.BoolChange || !args.Sig.BoolValue) return;
            if (_listeningToSigChanges)
            {
                _listeningToSigChanges = false;
                SigProvider.SigChange -= SigProviderOnSigChange;
            }
            OnVisibilityChanged(this,
                new VisibilityChangeEventArgs(this.RequestedVisibleState, VisibilityChangeEventType.AnimationComplete));
        }
    }
}