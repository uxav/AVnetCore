using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Config;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.UI.Ch5.MessageHandling;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class Ch5ApiHandlerBase
    {
        private static readonly Dictionary<string, Ch5ApiHandlerBase> Handlers =
            new Dictionary<string, Ch5ApiHandlerBase>();

        private readonly Core3ControllerBase _deviceController;

        private readonly Dictionary<int, EventSubscription> _eventSubscriptions =
            new Dictionary<int, EventSubscription>();

        protected Ch5ApiHandlerBase()
        {
            Logger.Debug($"Created instance of {GetType().FullName} for websocket connection");
        }

        protected Ch5ApiHandlerBase(Core3ControllerBase deviceController)
        {
            Logger.Debug($"Created instance of {GetType().FullName} for websocket connection");
            _deviceController = deviceController;
        }

        internal static IEnumerable<Ch5ApiHandlerBase> ConnectedHandlers =>
            new ReadOnlyCollectionBuilder<Ch5ApiHandlerBase>(Handlers.Values).ToReadOnlyCollection();

        public Ch5ConnectionInstance Connection { get; private set; }

        public event SendEventHandler SendEvent;

        private void Send(string data)
        {
            SendEvent?.Invoke(data);
        }

        internal void OnConnectInternal(Ch5ConnectionInstance connection)
        {
            Connection = connection;
            Handlers.Add(connection.ID, this);
            var rooms = new List<object>();
            foreach (var room in UxEnvironment.GetRooms())
                rooms.Add(new
                {
                    room.Id,
                    room.Name
                });

            SendNotification("hello", new
            {
                type = _deviceController != null ? ControllerType.Ch5Device.ToString() : ControllerType.Web.ToString(),
                guid = Ch5WebSocketServer.RuntimeGuid,
                controller = _deviceController?.Id ?? 0,
                ipid = _deviceController?.Device.ID ?? 0,
                defaultRoom = _deviceController?.DefaultRoom?.Id ?? 0,
                room = _deviceController?.Room?.Id ?? 0,
                rooms
            });
            EventService.EventOccured += EventServiceOnEventOccured;
            try
            {
                OnConnect(connection);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal void OnDisconnectInternal(Ch5ConnectionInstance connection)
        {
            EventService.EventOccured -= EventServiceOnEventOccured;
            Handlers.Remove(connection.ID);
            lock (_eventSubscriptions)
            {
                foreach (var subscription in _eventSubscriptions.Values)
                {
                    subscription.Unsubscribe();
                }

                _eventSubscriptions.Clear();
            }

            try
            {
                OnDisconnect();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void EventServiceOnEventOccured(EventMessage message)
        {
            try
            {
                switch (message.MessageType)
                {
                    case EventMessageType.LogEntry:
                    case EventMessageType.SessionExpired:
                    case EventMessageType.ProgramStopping:
                    case EventMessageType.BootStatus:
                        return;
                    default:
                        SendNotification($"EventService:{message.MessageType}", message.Message);
                        return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void OnConnect(Ch5ConnectionInstance connection);

        protected abstract void OnDisconnect();

        protected void SendNotification(string method, params object[] args)
        {
            var msg = new NotificationMessage(method, args);
            Send(msg.ToString());
        }

        protected void SendNotification(string method, object namedArgsObject = null)
        {
            var msg = new NotificationMessage(method, namedArgsObject);
            Send(msg.ToString());
        }

        internal void SendNotificationInternal(string method, params object[] args)
        {
            SendNotification(method, args);
        }

        internal void SendNotificationInternal(string method, object namedArgsObject)
        {
            SendNotification(method, namedArgsObject);
        }

        internal void OnReceiveInternal(JToken data)
        {
            Task.Run(() =>
            {
                try
                {
                    var request = new RequestMessage(data);
                    var response = ProcessResponse(request);
                    Send(response.ToString());
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    var response = data["id"] != null
                        ? new ResponseMessage(data["id"].Value<int>(), e)
                        : new ResponseMessage(e);
                    Send(response.ToString());
                }
            });
        }

        private ResponseMessage ProcessResponse(RequestMessage message)
        {
            if (message.Id == null)
            {
                throw new NullReferenceException("id cannot be null");
            }
            try
            {
                var result = FindAndInvokeMethod<ApiTargetMethodAttribute>(message.Method, message.RequestParams);
                return new ResponseMessage((int) message.Id, result);
            }
            catch (TargetInvocationException e)
            {
                Logger.Error(e.InnerException);
                return new ResponseMessage((int) message.Id, e.InnerException);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return new ResponseMessage((int) message.Id, e);
            }
        }

        private object FindAndInvokeMethod<T>(string method, JToken args) where T : ApiTargetAttributeBase
        {
            var methods = GetType().GetMethods();
            foreach (var methodInfo in methods)
            {
                var attribute = methodInfo.GetCustomAttribute<T>();
                //Logger.Debug($"Looking at method: {method.Name}");
                if (attribute == null || attribute.Name != method) continue;
                //Logger.Debug($"Name matches....");
                var methodParams = methodInfo.GetParameters();
                //Logger.Debug($"Param count = {methodParams.Length}");
                switch (args)
                {
                    case null when methodParams.Length == 0:
                    {
                        var result = methodInfo.Invoke(this, new object[] { });
                        if (methodInfo.ReturnType == typeof(void)) result = true;
                        return result;
                    }
                    case null:
                        continue;
                    default:
                    {
                        if (args.Count() != methodParams.Length) continue;
                        var invokeParams = (from methodParam in methodParams
                                where args[methodParam.Name] != null
                                // ReSharper disable once PossibleNullReferenceException
                                select args[methodParam.Name].ToObject(methodParam.ParameterType))
                            .ToArray();
                        if (invokeParams.Length != methodParams.Length) continue;
                        var result = methodInfo.Invoke(this, invokeParams);
                        if (methodInfo.ReturnType == typeof(void)) result = true;
                        return result;
                    }
                }
            }

            throw new MissingMethodException(GetType().FullName, method);
        }

        [ApiTargetMethod("Log")]
        public void Log(string message)
        {
            Logger.Highlight($"WS Connection Log: {message}");
        }

        [ApiTargetMethod("Subscribe")]
        public void Subscribe(int id, string name, JToken @params)
        {
            //Logger.Log($"Subscribe with id: {id}, name: {name}, params: {@params}");
            lock (_eventSubscriptions)
            {
                if (_eventSubscriptions.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Event ID {id} already registered");
                }
            }

            var obj = FindAndInvokeMethod<ApiTargetEventAttribute>(name, @params);
            var attribute = GetType().GetMethods()
                .First(m => m.GetCustomAttribute<ApiTargetEventAttribute>()?.Name == name)
                .GetCustomAttribute<ApiTargetEventAttribute>();
            var ctor =
                attribute.SubscriptionType.GetConstructor(new[]
                    { typeof(Ch5ApiHandlerBase), typeof(int), typeof(string), typeof(object), typeof(string) });
            var sub = (EventSubscription)ctor.Invoke(new[] { this, id, name, obj, attribute.EventName });
            lock (_eventSubscriptions)
            {
                _eventSubscriptions[id] = sub;
            }
        }

        [ApiTargetMethod("Unsubscribe")]
        public void Unsubscribe(int id)
        {
            //Logger.Log($"Unsubscribe with id: {id}");
            lock (_eventSubscriptions)
            {
                if (!_eventSubscriptions.ContainsKey(id))
                {
                    throw new KeyNotFoundException($"Subscription with ID {id} does not exist");
                }
            }

            lock (_eventSubscriptions)
            {
                var sub = _eventSubscriptions[id];
                sub.Unsubscribe();
                _eventSubscriptions.Remove(id);
            }
        }

        [ApiTargetMethod("GetCsInfo")]
        public object GetCsInfo()
        {
            return new
            {
                SystemBase.IpAddress,
                SystemBase.HostName,
                SystemBase.DomainName,
                SystemBase.MacAddress,
                SystemBase.DevicePlatform,
                InitialParametersClass.RoomId,
                InitialParametersClass.RoomName,
                SystemBase.ProgramApplicationDirectory,
                SystemBase.DhcpStatus,
                ConfigManager.ConfigPath,
                UpTime = SystemBase.UpTime.ToPrettyFormat(),
                Ch5WebSocketServer.WebSocketBaseUrl,
            };
        }
    }

    public delegate void SendEventHandler(string data);

    public enum ControllerType
    {
        Ch5Device,
        Web
    }
}