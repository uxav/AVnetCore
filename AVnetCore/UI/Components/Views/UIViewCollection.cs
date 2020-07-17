using System.Collections.Generic;
using System.Linq;
using UXAV.AVnetCore.Models.Collections;

namespace UXAV.AVnetCore.UI.Components.Views
{
    public class UIViewCollection<T> : UXCollection<T> where T : UISubPageViewController
    {
        public UIViewCollection()
        {
        }

        public UIViewCollection(IEnumerable<T> fromViews)
            : base(fromViews)
        {
        }

        public void ShowOnly(uint id)
        {
            foreach (var item in InternalDictionary)
            {
                if (item.Key != id && item.Value.Visible)
                {
                    item.Value.Hide();
                }
            }

            InternalDictionary[id].Show();
        }

        public void HideAll()
        {
            foreach (var viewController in InternalDictionary.Values.Where(v => v.Visible))
            {
                viewController.Hide();
            }
        }
    }
}