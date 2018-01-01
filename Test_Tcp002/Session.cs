using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Tcp002
{
    public struct Session
    {
        public string SessionId { get; set; }
        public DateTime LastAccessTime { get; set; }
    }
}
