using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using TcpServer;

namespace testApp
{
    public partial class ServerBoardForm : Form
    {
        private delegate void InvokeDelegate();

        private static string _nl = Environment.NewLine;

        public ServerBoardForm()
        {
            InitializeComponent();
        }

        private void _start_Click(object sender, EventArgs e)
        {
            _server.Close();
            _server.Port = 4444;
            _server.Open();
            _messageArea.AppendText($"Listening on {_server.Port}...{_nl}");
        }

        private void _send_Click(object sender, EventArgs e)
        {
            _server.Send( $"{_message.Text}{_nl}");
            _message.Text = string.Empty;
        }

        private void ServerBoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _server.Close();
        }

        private void _server_OnConnect(TcpServerConnection connection)
        {
            _server.Send($"Welcome!{_nl}");
            InvokeDelegate del = () =>
            {
                EndPoint ep = connection.Socket.Client.RemoteEndPoint;
                handleInput($"Remote endpoint : {ep}{_nl}");
            };
            Invoke(del);
        }

        private void _server_OnDataAvailable(TcpServerConnection connection)
        {
            byte[] data = ReadStream(connection.Socket);

            if (data != null)
            {
                string dataStr = Encoding.ASCII.GetString(data);

                InvokeDelegate del = () =>
                {
                    handleInput($"{dataStr}{_nl}");
                };
                Invoke(del);

                data = null;
            }
        }

        private void handleInput(string dataStr)
        {
            _messageArea.AppendText(dataStr);
        }

        private void _server_OnError(TcpServer.TcpServer server, Exception e)
        {

        }

        private byte[] ReadStream(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            if (stream.DataAvailable)
            {
                byte[] data = new byte[client.Available];

                int bytesRead = 0;
                try
                {
                    bytesRead = stream.Read(data, 0, data.Length);
                }
                catch (IOException)
                {
                }

                if (bytesRead < data.Length)
                {
                    byte[] lastData = data;
                    data = new byte[bytesRead];
                    Array.ConstrainedCopy(lastData, 0, data, 0, bytesRead);
                }
                return data;
            }
            return null;
        }

    }
}
