namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IFusionAsset : IAsset
    {
        FusionAssetType FusionAssetType { get; }
    }

    public enum FusionAssetType
    {
        TouchPanel,
        Display,
        VideoConferenceCodec,
        AudioProcessor,
        IpTvReceiver,
        Camera,
        VideoSwitcher,
        AirMedia,
        NvxEndpoint
    }
}