using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Tcp002
{
    public static class CustomProtocol
    {
        public const string SESSIONID = "ID";
        public const string COMMANDHELO = "HELO";
        public const string COMMANDECHO = "ECO";
        public const string COMMANDREV = "REV";
        public const string COMMANDBYE = "BYE";
        public const string COMMANDSET = "SET";
        public const string COMMANDGET = "GET";

        public const string STATUSOK = "OK";
        public const string STATUSCLOSED = "CLOSE";
        public const string STATUSINVALID = "INV";
        public const string STATUSUNKNOWN = "UNK";
        public const string STATUSNOTFOUND = "NOTFOUND";
        public const string STATUSTIMEOUT = "TIMEOUT";

        public const string SEPARATOR = "::";

        public static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(2);
    }
}
