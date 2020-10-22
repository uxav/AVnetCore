namespace UXAV.AVnetCore.DeviceSupport
{
    public interface ICamera
    {
        void TiltUp();
        void TiltDown();
        void TiltStop();
        void PanLeft();
        void PanRight();
        void PanStop();
        void ZoomIn();
        void ZoomOut();
        void ZoomStop();
        void ResetPosition();
        void RecallDefaultPosition();
    }
}