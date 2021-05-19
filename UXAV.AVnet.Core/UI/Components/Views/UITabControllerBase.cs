using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.DeviceSupport;

namespace UXAV.AVnet.Core.UI.Components.Views
{
    public abstract class UITabControllerBase : UIViewControllerBase
    {
        private readonly UITabBar _tabBar;
        private readonly UIViewCollection<UISubPageViewController> _views;
        private readonly Dictionary<uint, uint> _viewsForTabButtons = new Dictionary<uint, uint>();

        protected UITabControllerBase(ISigProvider sigProvider, uint visibleJoinNumber, SmartObject tabBarSmartObject)
            : base(sigProvider, visibleJoinNumber)
        {
            _tabBar = new UITabBar(tabBarSmartObject);
            _views = new UIViewCollection<UISubPageViewController>();
        }

        protected virtual void ShowDefaultView()
        {
            _views.FirstOrDefault()?.Show();
        }

        protected override void WillShow()
        {
            ShowDefaultView();
            _tabBar.ButtonEvent += OnTabBarButtonEvent;
        }

        protected override void DidShow()
        {
        }

        protected override void WillHide()
        {
            _tabBar.ButtonEvent += OnTabBarButtonEvent;
        }

        protected override void DidHide()
        {
        }

        internal void AddView(uint tabButton, UISubPageViewController view)
        {
            _views.Add(view);
            _viewsForTabButtons[tabButton] = view.Id;
            view.VisibilityChanged += ViewOnVisibilityChanged;
        }

        private void ViewOnVisibilityChanged(IVisibleItem item, VisibilityChangeEventArgs args)
        {
            if(args.EventType != VisibilityChangeEventType.WillShow) return;
            foreach (var kvp in _viewsForTabButtons.Where(kvp => kvp.Value == item.VisibleJoinNumber))
            {
                _tabBar.SetInterlockedFeedback(kvp.Key);
                return;
            }
        }

        protected void OnTabBarButtonEvent(IButton button, ButtonEventArgs args)
        {
            if(args.EventType != ButtonEventType.Released) return;
            if (_viewsForTabButtons.ContainsKey(args.CollectionKey))
            {
                _views.ShowOnly(_viewsForTabButtons[args.CollectionKey]);
            }
        }
    }
}