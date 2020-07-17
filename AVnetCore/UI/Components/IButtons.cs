using System.Collections.Generic;

namespace UXAV.AVnetCore.UI.Components
{
    public interface IButtons
    {
        IEnumerable<IButton> Buttons { get; }
    }
}