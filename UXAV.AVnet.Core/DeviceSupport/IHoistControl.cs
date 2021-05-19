namespace UXAV.AVnet.Core.DeviceSupport
{
    public interface IHoistControl
    {
        void Up();
        void Down();
        void Stop();
    }
}