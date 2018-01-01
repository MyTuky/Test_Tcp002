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
        public void Run()
        {
            using (var timer = new Timer(TimerSessionCleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)))
            {
                RunServerAsync().Wait();
            }
        }
        private async Task RunServerAsync()
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, portNumber);
                Console.WriteLine(string.Format("listener started at port {0}", portNumber));//($"listener started at port {portNumber}");
                listener.Start();

                while (true)
                {
                    Console.WriteLine("Waiting for client...");
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Task t = RunClientRequest(client);
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
                            using (NetworkStream stream = client.GetStream())
                            {
                                bool completed = false;
                                do
                                {
                                    byte[] readBuffer = new byte[1024];
                                    int read = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                                    string request = Encoding.ASCII.GetString(readBuffer, 0, read);
                                    Console.WriteLine(string.Format("received {0}", request));

                                    string sessionId;
                                    string result;
                                    byte[] writeBuffer = null;
                                    string response = string.Empty;

                                    ParseResponse resp = ParseRequest(request, out sessionId, out result);
                                    switch (resp)
                                    {
                                        case ParseResponse.OK:
                                            string content = string.Format("{0}::{1}::{2}", CustomProtocol.STATUSOK, CustomProtocol.SESSIONID, sessionId);
                                            if (!string.IsNullOrEmpty(result))
                                            {
                                                content += string.Format("{0}{1}", CustomProtocol.SEPARATOR, result);
                                            }
                                            response = string.Format("{0}{1}{2}{3}{4}{5}{6}", CustomProtocol.STATUSOK, CustomProtocol.SEPARATOR, CustomProtocol.SESSIONID, CustomProtocol.SEPARATOR, sessionId, CustomProtocol.SEPARATOR, content);
                                            break;
                                        case ParseReponse.CLOSE:
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
                                    await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
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
            string[] requestColl = request.Split(new string[] { CustomProtocol.SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

            if (requestColl[0] == CustomProtocol.COMMANDHELO)//fitst request
            {
                sessionId = _sessionManager.CreateSession();
            }
            else if (requestColl[0] == CustomProtocol.SESSIONID)//any other valid request
            {
                sessionId = requestColl[1];

                if (!_sessionManager.TouchSession(sessionId))
                {
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
            {
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
