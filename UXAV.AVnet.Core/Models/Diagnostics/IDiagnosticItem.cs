using System.Collections.Generic;

namespace UXAV.AVnet.Core.Models.Diagnostics
{
    public interface IDiagnosticItem
    {
        IEnumerable<DiagnosticMessage> GetMessages();
    }
}