using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronAuthentication;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.WebScripting
{
    public static class AppAuthentication
    {
        private static readonly Hashtable Sessions;
        private static readonly EventWaitHandle WaitHandle;
        private static bool _updated;
        private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        static AppAuthentication()
        {
            try
            {
                Sessions = LoadData();
                ClearOldSessions();
                WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type == eProgramStatusEventType.Stopping)
                    {
                        WaitHandle.Set();
                    }
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
                if (signalled) return;

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
                var keys = Sessions.Keys.Cast<string>().ToArray();
                var now = DateTime.Now;
                var expiredSessions =
                    (from key in keys
                        let date = ((Session) Sessions[key]).ExpiryTime
                        let expired = DateTime.Now > date
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
                session = (Session) Sessions[sessionId];
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

                if (!renew) return session;

                lock (Sessions)
                {
                    var newExpiry = DateTime.Now + TimeSpan.FromHours(12);
                    session.ExpiryTime = newExpiry;
                    _updated = true;
                    //Logger.Debug("Session {0} extended to {1}", session.SessionId, session.ExpiryTime);
                    return session;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw new UnauthorizedAccessException(e.GetType().Name);
            }
        }

        private static Hashtable LoadData()
        {
            if (!File.Exists(@"/nvram/sessions.bin")) return new Hashtable();
            Hashtable data = null;
            _lock.EnterReadLock();
            try
            {
                var formatter = new BinaryFormatter();
                using (var fs = File.OpenRead(@"/nvram/sessions.bin"))
                {
                    data = (Hashtable) formatter.Deserialize(fs);
                }
            }
            catch
            {
                data = new Hashtable();
            }
            _lock.ExitReadLock();
            return data;
        }

        private static void SaveData(Hashtable data)
        {
            _lock.EnterWriteLock();
            var formatter = new BinaryFormatter();
            using (var fs = File.Create(@"/nvram/sessions.bin"))
            {
                formatter.Serialize(fs, data);
            }

            _updated = false;
            _lock.ExitWriteLock();
        }

        public static Session StartSession(string username, string password, bool stayLoggedIn = false)
        {
            var userToken = Authentication.GetAuthenticationToken(username, password);
            if (!userToken.Valid) throw new UnauthorizedAccessException();
            var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("=", "")
                .Replace("+", "");
            var expiry = DateTime.Now + TimeSpan.FromMinutes(30);
            if (stayLoggedIn)
            {
                expiry = DateTime.Now + TimeSpan.FromDays(30);
            }
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

        public override string ToString()
        {
            return $"Session for \"{Name}\" Expires: {ExpiryTime}";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(SessionId), SessionId);
            info.AddValue(nameof(ExpiryTime), ExpiryTime);
        }
    }
}