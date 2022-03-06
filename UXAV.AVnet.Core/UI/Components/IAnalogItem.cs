namespace UXAV.AVnet.Core.UI.Components
{
    public interface IAnalogItem
    {
        uint AnalogJoinNumber { get; }
        ushort Value { get; set; }
        short SignedValue { get; set; }
        double Position { get; set; }
        void SetValue(ushort value);
        void SetSignedValue(short value);
        void SetPosition(double position);
    }
}