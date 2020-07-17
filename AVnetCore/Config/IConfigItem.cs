namespace UXAV.AVnetCore.Config
{
    public interface IConfigItem
    {
        bool Enabled { get; set; }
        string Name { get; set; }
        string Description { get; set; }
    }
}