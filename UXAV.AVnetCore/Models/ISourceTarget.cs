using System.Threading.Tasks;
using UXAV.AVnetCore.Models.Sources;

namespace UXAV.AVnetCore.Models
{
    public interface ISourceTarget
    {
        string Name { get; }
        SourceBase GetCurrentSource(uint forIndex = 1);
        Task<bool> SelectSourceAsync(SourceBase source, uint forIndex = 1);
    }
}