using System;
using System.Threading.Tasks;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class AutoDiscoveryApiHandler : ApiRequestHandler
    {
        public AutoDiscoveryApiHandler(WebScriptingServer server, WebScriptingRequest request) : base(server, request)
        {
        }

        public async void Get()
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

                await WriteResponseAsync(results);
            }
            catch (OperationCanceledException e)
            {
                await HandleErrorAsync(503, e.Message);
            }
            catch (AggregateException e)
            {
                LogInnerExceptions(e);
                await HandleErrorAsync(500, e.Message);
            }
            catch (Exception e)
            {
                await HandleErrorAsync(500, e.Message);
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