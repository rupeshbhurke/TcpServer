using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TcpServer
{
    public interface ISender
    {
        void Send(string data);
    }
}
