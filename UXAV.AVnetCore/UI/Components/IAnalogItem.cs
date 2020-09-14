namespace UXAV.AVnetCore.UI.Components
{
    public interface IAnalogItem
    {
        uint AnalogJoinNumber { get; }
        void SetValue(ushort value);
        void SetSignedValue(short value);
        void SetPosition(double position);
        ushort Value { get; set; }
        short SignedValue { get; set; }
        double Position { get; set; }
    }
}