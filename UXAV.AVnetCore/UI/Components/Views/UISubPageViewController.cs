namespace UXAV.AVnetCore.UI.Components.Views
{
    public abstract class UISubPageViewController : UIViewControllerBase
    {
        protected UISubPageViewController(UIViewControllerBase sigProvider, uint visibleJoinNumber)
            : base(sigProvider, visibleJoinNumber)
        {
        }

        protected UISubPageViewController(UITabViewBase tabView, uint tabButtonNumber, uint visibleJoinNumber)
            : base(tabView, visibleJoinNumber)
        {
            tabView.AddView(tabButtonNumber, this);
        }
    }
}