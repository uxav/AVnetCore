using UXAV.AVnetCore.Models.Sources;

namespace UXAV.AVnetCore.Models
{
    public interface ISourceTarget
    {
        SourceBase Source { get; set; }
    }
}