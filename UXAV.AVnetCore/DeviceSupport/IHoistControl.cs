namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IHoistControl
    {
        void Up();
        void Down();
        void Stop();
    }
}