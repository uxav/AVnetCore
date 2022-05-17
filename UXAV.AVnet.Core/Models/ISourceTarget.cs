using System.Threading.Tasks;
using UXAV.AVnet.Core.Models.Sources;

namespace UXAV.AVnet.Core.Models
{
    public interface ISourceTarget : IUniqueId
    {
        string Name { get; }
        SourceBase GetCurrentSource(uint forIndex = 1);
        Task<bool> SelectSourceAsync(SourceBase source, uint forIndex = 1);
    }
}