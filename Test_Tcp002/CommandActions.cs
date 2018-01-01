using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Tcp002
{
    public class CommandActions
    {
        public string Reverse(string action) => string.Join("",action.Reverse());
        public string Echo(string action)=>action;
    }
}
