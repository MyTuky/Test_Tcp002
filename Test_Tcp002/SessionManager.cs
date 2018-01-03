using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Tcp002
{
    public class SessionManager
    {
        /// <summary>
     /// 以删除最古老的会话
     /// </summary>
        public struct Session
        {
            /// <summary>
            /// 标识符
            /// </summary>
            public string SessionId { get; set; }
            /// <summary>
            /// 上次访问会话的时间
            /// </summary>
            public DateTime LastAccessTime { get; set; }
        }
        /// <summary>
        /// 存储所有会话，使用多个客户端时字典可以在多个线程中同时访问（System.Collections.Concurrent）
        /// </summary>
        private readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();
        /// <summary>
        /// 存储所有会话数据，使用多个客户端时字典可以在多个线程中同时访问（System.Collections.Concurrent）
        /// </summary>
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _sessionData = new ConcurrentDictionary<string, Dictionary<string, string>>();
        /// <summary>
        /// 创建一个新的会话，并添加到字典中
        /// </summary>
        /// <returns></returns>
        public string CreateSession()
        {
            string sessionId = Guid.NewGuid().ToString();
            if (_sessions.TryAdd(sessionId, new Session
            {
                SessionId = sessionId,
                LastAccessTime = DateTime.UtcNow
            }))
            {
                return sessionId;
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 每分钟调用一次，删除最近没有使用的所有会话
        /// </summary>
        public void CleanupAllSessions()
        {
            foreach (var session in _sessions)
            {
                if (session.Value.LastAccessTime + CustomProtocol.SessionTimeout >= DateTime.UtcNow)
                {
                    CleanupSession(session.Key);
                }
            }
        }
        /// <summary>
        /// 删除单个会话
        /// </summary>
        /// <param name="sessionId"></param>
        public void CleanupSession(string sessionId)
        {
            Dictionary<string, string> removed;
            if (_sessionData.TryRemove(sessionId, out removed))
            {
                Console.WriteLine(string.Format("removed {0} from session data", sessionId));
            }
            Session header;
            if (_sessions.TryRemove(sessionId, out header))
            {
                Console.WriteLine(string.Format("removed {0} from sessions", sessionId));
            }
        }
        /// <summary>
        /// 更新会话的LastAccessTime，会话无效则返回false
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public bool TouchSession(string sessionId)
        {
            Session oldHeader;
            if (!_sessions.TryGetValue(sessionId, out oldHeader))
            {
                return false;
            }
            Session updatedHeader = oldHeader;
            updatedHeader.LastAccessTime = DateTime.UtcNow;
            _sessions.TryUpdate(sessionId, updatedHeader, oldHeader);
            return true;
        }
        /// <summary>
        /// 解析请求，以设置会话数据
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="requestAction"></param>
        /// <returns></returns>
        public string ParseSessionData(string sessionId, string requestAction)
        {
            string[] sessionData = requestAction.Split('=');//会话数据接收的动作包含由等号分隔的键和值
            if (sessionData.Length != 2)
            {
                return CustomProtocol.STATUSUNKNOWN;
            }
            string key = sessionData[0];
            string value = sessionData[1];
            SetSessionData(sessionId, key, value);
            return string.Format("{0}={1}", key, value);
        }
        /// <summary>
        /// 设置会话数据，添加或更新字典中的会话状态
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetSessionData(string sessionId, string key, string value)
        {
            Dictionary<string, string> data;
            if (!_sessionData.TryGetValue(sessionId, out data))
            {
                data = new Dictionary<string, string>();
                data.Add(key, value);
                _sessionData.TryAdd(sessionId, data);
            }
            else
            {
                string val;
                if (data.TryGetValue(key, out val))
                {
                    data.Remove(key);
                }
                data.Add(key, value);
            }
        }
        /// <summary>
        /// 检索值，或返回NOTFOUND
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetSessionData(string sessionId, string key)
        {
            Dictionary<string, string> data;
            if (_sessionData.TryGetValue(sessionId, out data))
            {
                string value;
                if (data.TryGetValue(key, out value))
                {
                    return value;
                }
            }
            return CustomProtocol.STATUSNOTFOUND;
        }
    }
}
