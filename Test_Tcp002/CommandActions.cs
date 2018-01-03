using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Tcp002
{
    public class CommandActions
    {
        /// <summary>
        /// 返回操作字符串
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public string Reverse(string action) => string.Join("", action.Reverse());
        /// <summary>
        /// 返回反向发送的字符串
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public string Echo(string action) => action;
    }
}
