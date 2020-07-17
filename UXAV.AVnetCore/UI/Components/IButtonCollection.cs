using System.Collections.Generic;

namespace UXAV.AVnetCore.UI.Components
{
    public interface IButtonCollection : IEnumerable<IButton>
    {
        event ButtonEventHandler ButtonEvent;
        void SetInterlockedFeedback(uint key);
        void ClearInterlockedFeedback();
    }
}