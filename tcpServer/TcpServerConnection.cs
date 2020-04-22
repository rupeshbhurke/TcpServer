/****************************************************************
 * This work is original work authored by Craig Baird, released *
 * under the Code Project Open Licence (CPOL) 1.02;             *
 * http://www.codeproject.com/info/cpol10.aspx                  *
 * This work is provided as is, no guarentees are made as to    *
 * suitability of this work for any specific purpose, use it at *
 * your own risk.                                               *
 * If this work is redistributed in code form this header must  *
 * be included and unchanged.                                   *
 * Any modifications made, other than by the original author,   *
 * shall be listed below.                                       *
 * Where applicable any headers added with modifications shall  *
 * also be included.                                            *
 ****************************************************************/

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer
{
    public class TcpServerConnection : ISender
    {
        private TcpClient _socket;
        private readonly List<byte[]> _messagesToSend;
        private int _attemptCount;

        private Thread _thread;

        private DateTime _lastVerifyTime;

        private Encoding _encoding;

        public TcpServerConnection(TcpClient sock, Encoding encoding)
        {
            _socket = sock;
            _messagesToSend = new List<byte[]>();
            _attemptCount = 0;

            _lastVerifyTime = DateTime.UtcNow;
            _encoding = encoding;
        }

        public bool Connected()
        {
            try
            {
                return _socket.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool VerifyConnected()
        {
            //note: `Available` is checked before because it's faster,
            //`Available` is also checked after to prevent a race condition.
            bool connected = _socket.Client.Available != 0 || 
                !_socket.Client.Poll(1, SelectMode.SelectRead) || 
                _socket.Client.Available != 0;
            _lastVerifyTime = DateTime.UtcNow;
            return connected;
        }

        public bool ProcessOutgoing(int maxSendAttempts)
        {
            lock (_socket)
            {
                if (!_socket.Connected)
                {
                    _messagesToSend.Clear();
                    return false;
                }

                if (_messagesToSend.Count == 0)
                {
                    return false;
                }

                NetworkStream stream = _socket.GetStream();
                try
                {
                    stream.Write(_messagesToSend[0], 0, _messagesToSend[0].Length);

                    lock (_messagesToSend)
                    {
                        _messagesToSend.RemoveAt(0);
                    }
                    _attemptCount = 0;
                }
                catch (System.IO.IOException)
                {
                    //occurs when there's an error writing to network
                    _attemptCount++;
                    if (_attemptCount >= maxSendAttempts)
                    {
                        //TODO log error

                        lock (_messagesToSend)
                        {
                            _messagesToSend.RemoveAt(0);
                        }
                        _attemptCount = 0;
                    }
                }
                catch (ObjectDisposedException)
                {
                    //occurs when stream is closed
                    _socket.Close();
                    return false;
                }
            }

            lock (_messagesToSend)
            {
                return _messagesToSend.Count != 0;
            }
        }

        public void SendData(string data)
        {
            byte[] array = _encoding.GetBytes(data);
            lock (_messagesToSend)
            {
                _messagesToSend.Add(array);
            }
        }

        public void ForceDisconnect()
        {
            lock (_socket)
            {
                _socket.Close();
            }
        }

        public bool HasMoreWork()
        {
            lock (_messagesToSend)
            {
                return _messagesToSend.Count > 0 || (Socket.Available > 0 && CanStartNewThread());
            }
        }

        private bool CanStartNewThread()
        {
            if (_thread == null)
            {
                return true;
            }
            return (_thread.ThreadState & (ThreadState.Aborted | ThreadState.Stopped)) != 0 &&
                   (_thread.ThreadState & ThreadState.Unstarted) == 0;
        }

        public TcpClient Socket
        {
            get
            {
                return _socket;
            }
            set
            {
                _socket = value;
            }
        }

        public Thread CallbackThread
        {
            get
            {
                return _thread;
            }
            set
            {
                if (!CanStartNewThread())
                {
                    throw new Exception("Cannot override TcpServerConnection Callback Thread. The old thread is still running.");
                }
                _thread = value;
            }
        }

        public DateTime LastVerifyTime
        {
            get
            {
                return _lastVerifyTime;
            }
        }

        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        public void Send(string data)
        {
            SendData(data);
        }
    }
}
