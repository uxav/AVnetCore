namespace UXAV.AVnet.Core.UI.Ch5
{
    public class ApiTargetMethodAttribute : ApiTargetAttributeBase
    {
        public ApiTargetMethodAttribute(string name)
        {
            Name = name;
        }

        public override string Name { get; }
    }
}