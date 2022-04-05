using System;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class ApiTargetAttributeBase : Attribute
    {
        public abstract string Name { get; }
    }
}