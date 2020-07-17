using System.Collections.Generic;

namespace UXAV.AVnetCore.Models.Diagnostics
{
    public interface IDiagnosticItem
    {
        IEnumerable<DiagnosticMessage> GetMessages();
    }
}