extern alias doNotUse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronAuthentication;
using doNotUse::Newtonsoft.Json;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.WebScripting
{
    public static class AppAuthentication
    {
        private static readonly Dictionary<string, Session> Sessions;
        private static readonly EventWaitHandle WaitHandle;
        private static bool _updated;
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        static AppAuthentication()
        {
            try
            {
                Sessions = LoadData();
                ClearOldSessions();
                WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type == eProgramStatusEventType.Stopping) WaitHandle.Set();
                };
                ThreadPool.QueueUserWorkItem(ManageSessions);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }

        private static void ManageSessions(object state)
        {
            while (true)
            {
                var signalled = WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
                if (signalled)
                {
                    Logger.Warn("Leaving app authentication thread");
                    return;
                }

                if (!ClearOldSessions() && !_updated) continue;
                lock (Sessions)
                {
                    SaveData(Sessions);
                }
            }
        }

        private static bool ClearOldSessions()
        {
            lock (Sessions)
            {
                //Logger.Debug("Cleaning up sessions...");
                var keys = Sessions.Keys.ToArray();
                var now = DateTime.Now;
                var expiredSessions =
                    (from key in keys
                        let date = Sessions[key].ExpiryTime
                        let expired = now > date
                        where expired
                        select key).ToArray();

                foreach (var key in expiredSessions)
                {
                    //Logger.Warn("Removing: " + key);
                    Sessions.Remove(key);
                    EventService.Notify(EventMessageType.SessionExpired, key);
                }

                return expiredSessions.Any();
            }
        }

        public static Session ValidateSession(string sessionId, bool renew)
        {
            Session session;
            lock (Sessions)
            {
                if (!Sessions.ContainsKey(sessionId)) return null;
                session = Sessions[sessionId];
            }

            try
            {
                var date = session.ExpiryTime;
                if (date < DateTime.Now)
                {
                    lock (Sessions)
                    {
                        Sessions.Remove(sessionId);
                    }

                    return null;
                }

                if (!renew || session.ExpiryTime > DateTime.Now + TimeSpan.FromMinutes(1)) return session;

                lock (Sessions)
                {
                    var newExpiry = DateTime.Now + TimeSpan.FromMinutes(30);
                    session.ExpiryTime = newExpiry;
                    _updated = true;
                    Logger.Debug("Session {0} extended to {1}", session.SessionId, session.ExpiryTime);
                    return session;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw new UnauthorizedAccessException(e.GetType().Name);
            }
        }

        private static Dictionary<string, Session> LoadData()
        {
            if (!File.Exists(@"/nvram/sessions.json")) return new Dictionary<string, Session>();
            Lock.EnterReadLock();
            try
            {
                using var fs = File.OpenText(@"/nvram/sessions.json");
                var content = fs.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, Session>>(content);
            }
            catch (Exception e)
            {
                Logger.Error($"Error loading sessions: {e.Message}");
                return new Dictionary<string, Session>();
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        private static void SaveData(Dictionary<string, Session> data)
        {
            try
            {
                Lock.EnterWriteLock();
                var json = JsonConvert.SerializeObject(data);
                File.WriteAllText(@"/nvram/sessions.json", json);
                _updated = false;
            }
            catch (Exception e)
            {
                Logger.Error($"Error saving sessions: {e.Message}");
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static Session StartSession(string username, string password, bool stayLoggedIn = false)
        {
            var userToken = Authentication.GetAuthenticationToken(username, password);
            if (!userToken.Valid) throw new UnauthorizedAccessException();
            var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("=", "")
                .Replace("+", "");
            var expiry = DateTime.Now + TimeSpan.FromMinutes(30);
            if (stayLoggedIn) expiry = DateTime.Now + TimeSpan.FromDays(30);
            var session = new Session(sessionId, userToken.UserName, expiry);
            Logger.Log("Created new WebApp Session {0}, Expires {1}", sessionId, expiry.ToString("R"));
            lock (Sessions)
            {
                Sessions.Add(sessionId, session);
                SaveData(Sessions);
                //Logger.Log("Sessions count = {0}", Sessions.Count);
            }

            return session;
        }

        public static void InvalidateSession(string sessionId)
        {
            lock (Sessions)
            {
                if (!Sessions.ContainsKey(sessionId)) return;
                Sessions.Remove(sessionId);
                SaveData(Sessions);
            }
        }
    }

    [Serializable]
    public class Session : ISerializable
    {
        public Session(string sessionId, string name, DateTime expiryTime)
        {
            SessionId = sessionId;
            Name = name;
            ExpiryTime = expiryTime;
        }

        protected Session(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(nameof(Name));
            SessionId = info.GetString(nameof(SessionId));
            ExpiryTime = DateTime.Parse(info.GetString(nameof(ExpiryTime)));
        }

        public string Name { get; }
        public string SessionId { get; }
        public DateTime ExpiryTime { get; set; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(SessionId), SessionId);
            info.AddValue(nameof(ExpiryTime), ExpiryTime);
        }

        public override string ToString()
        {
            return $"Session for \"{Name}\" Expires: {ExpiryTime}";
        }
    }
}