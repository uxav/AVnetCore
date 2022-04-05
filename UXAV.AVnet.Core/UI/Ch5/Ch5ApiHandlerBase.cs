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

        private Dictionary<int, EventSubscription> _subscriptions = new Dictionary<int, EventSubscription>();

        private readonly Core3ControllerBase _deviceController;

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

        protected void SendNotification(string method, object namedArgsObject)
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
                        ? new ResponseMessage(data["id"].Value<string>(), e)
                        : new ResponseMessage(e);
                    Send(response.ToString());
                }
            });
        }

        private ResponseMessage ProcessResponse(RequestMessage message)
        {
            var methods = GetType().GetMethods();
            foreach (var methodInfo in methods)
            {
                var attribute = methodInfo.GetCustomAttribute<ApiTargetMethodAttribute>();
                //Logger.Debug($"Looking at method: {method.Name}");
                if (attribute == null || attribute.Name != message.Method) continue;
                //Logger.Debug($"Name matches....");
                var methodParams = methodInfo.GetParameters();
                //Logger.Debug($"Param count = {methodParams.Length}");
                switch (message.RequestParams)
                {
                    case null when methodParams.Length == 0:
                    {
                        try
                        {
                            var result = methodInfo.Invoke(this, new object[] { });
                            if (methodInfo.ReturnType == typeof(void)) result = true;
                            return new ResponseMessage(message.Id, result);
                        }
                        catch (TargetInvocationException e)
                        {
                            Logger.Error(e.InnerException);
                            return new ResponseMessage(message.Id, e.InnerException);
                        }
                    }
                    case null:
                        continue;
                    default:
                    {
                        if (message.RequestParams.Count() != methodParams.Length) continue;
                        var invokeParams = (from methodParam in methodParams
                                where message.RequestParams[methodParam.Name] != null
                                // ReSharper disable once PossibleNullReferenceException
                                select message.RequestParams[methodParam.Name].ToObject(methodParam.ParameterType))
                            .ToArray();
                        if (invokeParams.Length != methodParams.Length) continue;
                        try
                        {
                            var result = methodInfo.Invoke(this, invokeParams);
                            if (methodInfo.ReturnType == typeof(void)) result = true;
                            return new ResponseMessage(message.Id, result);
                        }
                        catch (TargetInvocationException e)
                        {
                            Logger.Error(e.InnerException);
                            return new ResponseMessage(message.Id, e.InnerException);
                        }
                    }
                }
            }

            throw new MissingMethodException(GetType().FullName, message.Method);
        }

        [ApiTargetMethod("Subscribe")]
        public void Subscribe(string name, int id)
        {
            Logger.Debug($"Subscribe to \"{name}\", id = {id}");
            lock (_subscriptions)
            {
                if (_subscriptions.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Subscription with ID {id} already exists");
                }
            }

            var events = GetType().GetEvents();
            foreach (var eventInfo in events)
            {
                var attribute = eventInfo.GetCustomAttribute<ApiTargetEventAttribute>();
                //Logger.Debug($"Looking at method: {method.Name}");
                if (attribute == null || attribute.Name != name) continue;
                var sub = new EventSubscription(this, name, id);
                lock (_subscriptions)
                {
                    _subscriptions[id] = sub;
                }

                var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, sub, "Notify");
                eventInfo.AddEventHandler(this, handler);

                var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var methodInfo in methods)
                {
                    attribute = methodInfo.GetCustomAttribute<ApiTargetEventAttribute>();
                    //Logger.Debug($"Looking at method: {method.Name}");
                    if (attribute == null || attribute.Name != name) continue;
                    var result = methodInfo.Invoke(this, new object[] { });
                    handler.Method.Invoke(sub, new[] { result });
                    break;
                }

                return;
            }

            throw new MissingMemberException(GetType().FullName, name);
        }

        [ApiTargetMethod("Unsubscribe")]
        public void Unsubscribe(int id)
        {
            Logger.Debug($"Unsubscribe from id = {id}");
            lock (_subscriptions)
            {
                if (!_subscriptions.ContainsKey(id))
                {
                    throw new KeyNotFoundException($"Subscription with ID {id} does not exist");
                }
            }

            string name;
            lock (_subscriptions)
            {
                name = _subscriptions[id].Name;
            }

            var events = GetType().GetEvents();
            foreach (var eventInfo in events)
            {
                var attribute = eventInfo.GetCustomAttribute<ApiTargetEventAttribute>();
                //Logger.Debug($"Looking at method: {method.Name}");
                if (attribute == null || attribute.Name != name) continue;
                lock (_subscriptions)
                {
                    var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, _subscriptions[id], "Notify");
                    eventInfo.RemoveEventHandler(this, handler);
                    _subscriptions.Remove(id);
                }

                return;
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

    public class ApiTargetMethodAttribute : Attribute
    {
        public ApiTargetMethodAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class ApiTargetEventAttribute : Attribute
    {
        public ApiTargetEventAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    internal class EventSubscription
    {
        private readonly Ch5ApiHandlerBase _handler;

        internal EventSubscription(Ch5ApiHandlerBase handler, string name, int id)
        {
            _handler = handler;
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }

        public void Notify(object eventInfo)
        {
            _handler.SendNotificationInternal("event", new
            {
                Name,
                Id,
                Value = eventInfo
            });
        }
    }

    public delegate void SubscriptionEventHandler(object eventInfo);

    public enum ControllerType
    {
        Ch5Device,
        Web
    }
}