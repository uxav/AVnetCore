using UXAV.AVnetCore.DeviceSupport;

namespace UXAV.AVnetCore.UI.Components.Views
{
    public abstract class UISubPageViewController : UIViewControllerBase
    {
        protected UISubPageViewController(ISigProvider sigProvider, uint visibleJoinNumber)
            : base(sigProvider, visibleJoinNumber)
        {
        }

        protected UISubPageViewController(UITabControllerBase tabController, uint tabButtonNumber, uint visibleJoinNumber)
            : base(tabController, visibleJoinNumber)
        {
            tabController.AddView(tabButtonNumber, this);
        }
    }
}