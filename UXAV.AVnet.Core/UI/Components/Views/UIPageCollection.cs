using System.Collections;
using System.Collections.Generic;

namespace UXAV.AVnet.Core.UI.Components.Views
{
    public class UIPageCollection : IEnumerable<UIPageViewController>
    {
        private static readonly Dictionary<uint, Dictionary<uint, UIPageViewController>> Pages =
            new Dictionary<uint, Dictionary<uint, UIPageViewController>>();

        private static readonly Dictionary<uint, List<UIPageViewController>> PreviousPagesDict =
            new Dictionary<uint, List<UIPageViewController>>();

        private readonly Core3ControllerBase _core3Controller;

        /// <summary>
        ///     The default Constructor.
        /// </summary>
        internal UIPageCollection(Core3ControllerBase core3Controller)
        {
            _core3Controller = core3Controller;
            Pages[_core3Controller.Id] = new Dictionary<uint, UIPageViewController>();
            PreviousPagesDict[_core3Controller.Id] = new List<UIPageViewController>();
        }

        public UIPageViewController this[uint pageJoin]
        {
            get => Pages[_core3Controller.Id][pageJoin];
            private set => Pages[_core3Controller.Id][pageJoin] = value;
        }

        internal List<UIPageViewController> PreviousPages => PreviousPagesDict[_core3Controller.Id];

        public IEnumerator<UIPageViewController> GetEnumerator()
        {
            return Pages[_core3Controller.Id].Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Page visibility changes can be subscribed to here
        /// </summary>
        public event VisibilityChangeEventHandler VisibilityChanged;

        internal void Add(UIPageViewController page)
        {
            this[page.PageNumber] = page;
            page.VisibilityChanged += PageOnVisibilityChanged;
        }

        private void PageOnVisibilityChanged(IVisibleItem item, VisibilityChangeEventArgs args)
        {
            VisibilityChanged?.Invoke(item, args);
        }

        public void ClearPreviousPageLogic()
        {
            PreviousPagesDict[_core3Controller.Id].Clear();
        }

        public void ClearPreviousPageLogic(UIPageViewController pageToSetAsPrevious)
        {
            PreviousPagesDict[_core3Controller.Id].Clear();
            PreviousPagesDict[_core3Controller.Id].Add(pageToSetAsPrevious);
        }
    }
}