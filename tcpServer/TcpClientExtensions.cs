using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TcpServer
{
    public static class TcpClientExtensions
    {
        public static byte[] ReadStream(this TcpClient client)
        {
            if (client == null) return null;
            NetworkStream stream = client.GetStream();
            if (!stream.DataAvailable) return null;

            byte[] data = new byte[client.Available];
            int bytesRead = 0;
            try
            {
                bytesRead = stream.Read(data, 0, data.Length);
            }
            catch (IOException)
            {}

            if (bytesRead < data.Length)
            {
                byte[] lastData = data;
                data = new byte[bytesRead];
                Array.ConstrainedCopy(lastData, 0, data, 0, bytesRead);
            }
            return data;
        }
    }
}
