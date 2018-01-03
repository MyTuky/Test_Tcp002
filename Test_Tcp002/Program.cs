using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test_Tcp002
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Program();

        }
        /// <summary>
        /// 启动一个计时器，每分钟清理一次所有的会话状态
        /// </summary>
        public void Run()
        {
            using (var timer = new Timer(TimerSessionCleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)))
            {
                RunServerAsync().Wait();//启动服务器
            }
        }
        private async Task RunServerAsync()
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, portNumber);//实例化监听器
                Console.WriteLine(string.Format("listener started at port {0}", portNumber));//($"listener started at port {portNumber}");
                listener.Start();//开始监听

                while (true)
                {
                    Console.WriteLine("Waiting for client...");
                    TcpClient client = await listener.AcceptTcpClientAsync();//等待客户端连接
                    Task t = RunClientRequestAsync(client);//RunClientRequest(client);//处理客户端请求
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Exception of type {0},Message:{1}", ex.GetType().Name, ex.Message));//$"Exception of type {ex.GetType().Name},Message:{ex.Message}");
            }
        }
        private Task RunClientRequestAsync(TcpClient client)
        {
            return Task.Run(async () =>
                {
                    try
                    {
                        using (client)
                        {
                            Console.WriteLine("client connected");
                            using (NetworkStream stream = client.GetStream())//返回NetworkStream
                            {
                                bool completed = false;
                                do
                                {
                                    byte[] readBuffer = new byte[1024];
                                    int read = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);//获取客户机的请求
                                    string request = Encoding.ASCII.GetString(readBuffer, 0, read);
                                    Console.WriteLine(string.Format("received {0}", request));

                                    string sessionId;
                                    string result;
                                    byte[] writeBuffer = null;
                                    string response = string.Empty;

                                    ParseResponse resp = ParseRequest(request, out sessionId, out result);//辅助方法
                                    switch (resp)
                                    {//根据结果创建客户端的回应
                                        case ParseResponse.OK:
                                            string content = string.Format("{0}::{1}::{2}", CustomProtocol.STATUSOK, CustomProtocol.SESSIONID, sessionId);
                                            if (!string.IsNullOrEmpty(result))
                                            {
                                                content += string.Format("{0}{1}", CustomProtocol.SEPARATOR, result);
                                            }
                                            response = string.Format("{0}{1}{2}{3}{4}{5}{6}", CustomProtocol.STATUSOK, CustomProtocol.SEPARATOR, CustomProtocol.SESSIONID, CustomProtocol.SEPARATOR, sessionId, CustomProtocol.SEPARATOR, content);
                                            break;
                                        case ParseResponse.CLOSE:
                                            response = string.Format("{0}", CustomProtocol.STATUSCLOSED);
                                            completed = true;
                                            break;
                                        case ParseResponse.TIMEOUT:
                                            response = string.Format("{0}", CustomProtocol.STATUSTIMEOUT);
                                            break;
                                        case ParseResponse.ERROR:
                                            response = string.Format("{0}", CustomProtocol.STATUSINVALID);
                                            break;
                                        default:
                                            break;
                                    }
                                    writeBuffer = Encoding.ASCII.GetBytes(response);
                                    await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);//返回给客户端
                                    await stream.FlushAsync();
                                    Console.WriteLine(string.Format("returned {0}", Encoding.ASCII.GetString(writeBuffer, 0, writeBuffer.Length)));
                                } while (!completed);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Exception in client request handing of type {0},Message:{1}", ex.GetType().Name, ex.Message));
                    }
                    Console.WriteLine("client disconnected");
                });
        }
        private ParseResponse ParseRequest(string request, out string sessionId, out string response)
        {
            sessionId = string.Empty;
            response = string.Empty;
            string[] requestColl = request.Split(new string[] { CustomProtocol.SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);//过滤会话标识符

            if (requestColl[0] == CustomProtocol.COMMANDHELO)//fitst request//server(HELO)第一次调用是不从客户端传递会话标识符的唯一调用
            {
                sessionId = _sessionManager.CreateSession();//是使用SessionManager创建的（UUID唯一值）
            }
            else if (requestColl[0] == CustomProtocol.SESSIONID)//any other valid request
            {//除第一次，之后的第一位都必须包含ID
                sessionId = requestColl[1];//第二位必须包含会话标识符

                if (!_sessionManager.TouchSession(sessionId))//会话有效则更新会话标识符的当前时间
                {//会话无效，则超时
                    return ParseResponse.TIMEOUT;
                }
                if (requestColl[2] == CustomProtocol.COMMANDBYE)
                {
                    return ParseResponse.CLOSE;
                }
                if (requestColl.Length > 4)
                {
                    response = ProcessRequest(requestColl);
                }
            }
            else
            {
                return ParseResponse.ERROR;
            }
            return ParseResponse.OK;
        }
        #region 猜的
        /// <summary>
        /// 存储和检索会话状态
        /// </summary>
        SessionManager _sessionManager = new SessionManager();
        /// <summary>
        /// 回应或反向传递收到的信息
        /// </summary>
        CommandActions _commandActions = new CommandActions();
        private readonly TimerCallback TimerSessionCleanup;
        private int portNumber;

        enum ParseResponse
        {
            OK,
            ERROR,
            CLOSE,
            TIMEOUT
        }
        #endregion
        private string ProcessRequest(string[] requestColl)
        {
            if (requestColl.Length > 4)
            {
                throw new ArgumentException("invalid length requestColl");
            }

            string sessionId = requestColl[1];
            string response = string.Empty;
            string requestCommand = requestColl[2];
            string requestAction = requestColl[3];

            switch (requestCommand)
            {//处理不同的请求
                case CustomProtocol.COMMANDECHO:
                    response = _commandActions.Echo(requestAction);
                    break;
                case CustomProtocol.COMMANDREV:
                    response = _commandActions.Reverse(requestAction);
                    break;
                case CustomProtocol.COMMANDSET:
                    response = _sessionManager.ParseSessionData(sessionId, requestAction);
                    break;
                case CustomProtocol.COMMANDGET:
                    response = string.Format("{0}", _sessionManager.GetSessionData(sessionId, requestAction));
                    break;
                default:
                    response = CustomProtocol.STATUSUNKNOWN;
                    break;
            }
            return response;
        }
    }
}
