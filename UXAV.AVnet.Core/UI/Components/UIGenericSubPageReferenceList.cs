using Crestron.SimplSharpPro;

namespace UXAV.AVnet.Core.UI.Components
{
    public class UIGenericSubPageReferenceList : UISubPageReferenceList
    {
        public UIGenericSubPageReferenceList(SmartObject smartObject, uint digitalJoinIncrement = 1,
            uint analogJoinIncrement = 1, uint serialJoinIncrement = 1)
            : base(smartObject, digitalJoinIncrement, analogJoinIncrement, serialJoinIncrement,
                (list, id) => new UIGenericSubPageReferenceListItem(list, id))
        {
            foreach (var item in this) ButtonCollection.Add(item as IButton);
        }
    }
}