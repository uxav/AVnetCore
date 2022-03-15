using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
            OnConnect(connection);
        }

        internal void OnDisconnectInternal(Ch5ConnectionInstance connection)
        {
            EventService.EventOccured -= EventServiceOnEventOccured;
            Handlers.Remove(connection.ID);
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
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ApiTargetMethodAttribute>();
                //Logger.Debug($"Looking at method: {method.Name}");
                if (attribute == null || attribute.Name != message.Method) continue;
                //Logger.Debug($"Name matches....");
                var methodParams = method.GetParameters();
                //Logger.Debug($"Param count = {methodParams.Length}");
                switch (message.RequestParams)
                {
                    case null when methodParams.Length == 0:
                    {
                        try
                        {
                            var result = method.Invoke(this, new object[] { });
                            if (method.ReturnType == typeof(void)) result = true;
                            return new ResponseMessage(message.Id, result);
                        }
                        catch (TargetInvocationException e)
                        {
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
                            var result = method.Invoke(this, invokeParams);
                            if (method.ReturnType == typeof(void)) result = true;
                            return new ResponseMessage(message.Id, result);
                        }
                        catch (TargetInvocationException e)
                        {
                            return new ResponseMessage(message.Id, e.InnerException);
                        }
                    }
                }
            }

            throw new MissingMethodException("No method found");
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

    public enum ControllerType
    {
        Ch5Device,
        Web
    }
}