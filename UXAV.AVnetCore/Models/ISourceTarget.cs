using System.Threading.Tasks;
using UXAV.AVnetCore.Models.Sources;

namespace UXAV.AVnetCore.Models
{
    public interface ISourceTarget
    {
        SourceBase CurrentSource { get; }
        Task<bool> SelectSourceAsync(SourceBase source);
    }
}