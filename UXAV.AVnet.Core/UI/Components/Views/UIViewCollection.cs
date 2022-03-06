using System.Collections.Generic;
using System.Linq;
using UXAV.AVnet.Core.Models.Collections;

namespace UXAV.AVnet.Core.UI.Components.Views
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
            if (!InternalDictionary.ContainsKey(id))
                throw new KeyNotFoundException($"Collection does not contain the view with ID: {id}");

            HideAllExcept(id);
            InternalDictionary[id].Show();
        }

        public void ShowOnly(T view)
        {
            if (!InternalDictionary.ContainsKey(view.Id))
                throw new KeyNotFoundException($"Collection does not contain the view with ID: {view.Id}");

            HideAllExcept(view);
            view.Show();
        }

        public void HideAll()
        {
            foreach (var viewController in InternalDictionary.Values.Where(v => v.Visible)) viewController.Hide();
        }

        public void HideAllExcept(uint id)
        {
            if (!InternalDictionary.ContainsKey(id))
                throw new KeyNotFoundException($"Collection does not contain the view with ID: {id}");

            foreach (var viewController in InternalDictionary.Values.Where(v => v.Visible && v.Id != id))
                viewController.Hide();
        }

        public void HideAllExcept(T view)
        {
            if (!InternalDictionary.ContainsKey(view.Id))
                throw new KeyNotFoundException($"Collection does not contain the view with ID: {view.Id}");

            foreach (var viewController in InternalDictionary.Values.Where(v => v.Visible && v != view))
                viewController.Hide();
        }
    }
}