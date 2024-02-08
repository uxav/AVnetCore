using System;
using System.Threading.Tasks;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    public class AutoDiscoveryApiHandler : ApiRequestHandler
    {
        public AutoDiscoveryApiHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        public void Get()
        {
            try
            {
                var results = Task.Run(() =>
                {
                    var crestronAutoDiscovery = AutoDiscovery.GetAsync();
                    var qsysDiscovery = QsysDiscoveryProtocol.DiscoverAsync();
                    Task.WaitAll(crestronAutoDiscovery, qsysDiscovery);
                    return Task.FromResult(new
                    {
                        crestron = crestronAutoDiscovery.Result,
                        qsys = qsysDiscovery.Result
                    });
                }).Result;

                WriteResponse(results);
            }
            catch (OperationCanceledException e)
            {
                HandleError(503, "Service Unavailable", e.Message);
            }
            catch (AggregateException e)
            {
                LogInnerExceptions(e);
                HandleError(500, "Server Error", e.Message);
            }
            catch (Exception e)
            {
                HandleError(500, "Server Error", e.Message);
                Logger.Error(e);
            }
        }
        
        private void LogInnerExceptions(Exception ex)
        {
            if (ex is AggregateException aggEx)
            {
                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    LogInnerExceptions(innerEx);
                }
            }
            else
            {
                Logger.Error(ex);
            }
        }
    }
}