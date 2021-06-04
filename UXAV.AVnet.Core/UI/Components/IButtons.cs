using System.Collections.Generic;

namespace UXAV.AVnet.Core.UI.Components
{
    public interface IButtons
    {
        IEnumerable<IButton> Buttons { get; }
    }
}