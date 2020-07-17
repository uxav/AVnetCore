using Crestron.SimplSharpPro;

namespace UXAV.AVnetCore.UI.Components
{
    public class UIGenericSubPageReferenceList : UISubPageReferenceList
    {
        public UIGenericSubPageReferenceList(SmartObject smartObject)
            : base(smartObject, 1, 1, 1,
                (list, id) => new UIGenericSubPageReferenceListItem(list, id))
        {
            foreach (var item in this)
            {
                ButtonCollection.Add(item as IButton);
            }
        }
    }
}