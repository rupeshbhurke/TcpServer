/****************************************************************
 * This work is original work authored by Craig Baird, released *
 * under the Code Project Open License (CPOL) 1.02;             *
 * http://www.codeproject.com/info/cpol10.aspx                  *
 * This work is provided as is, no guarantees are made as to    *
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
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer
{
    public delegate void TcpServerConnectionChanged(TcpServerConnection connection);
    public delegate void TcpServerError(TcpServer server, Exception e);

    public partial class TcpServer : Component
    {
        private List<TcpServerConnection> _connections;
        private TcpListener _listener;

        private Thread _listenThread;
        private Thread _sendThread;

        private bool _mIsOpen;

        private int _mPort;
        private int _mMaxSendAttempts;
        private int _mIdleTime;
        private int _mMaxCallbackThreads;
        private int _mVerifyConnectionInterval;
        private Encoding _mEncoding;

        private SemaphoreSlim _sem;
        private bool _waiting;

        private int _activeThreads;
        private readonly object _activeThreadsLock = new object();

        public event TcpServerConnectionChanged OnConnect = null;
        public event TcpServerConnectionChanged OnDataAvailable = null;
        public event TcpServerError OnError = null;

        public TcpServer()
        {
            InitializeComponent();

            Initialise();
        }

        public TcpServer(IContainer container)
        {
            container.Add(this);

            InitializeComponent();

            Initialise();
        }

        private void Initialise()
        {
            _connections = new List<TcpServerConnection>();
            _listener = null;

            _listenThread = null;
            _sendThread = null;

            _mPort = -1;
            _mMaxSendAttempts = 3;
            _mIsOpen = false;
            _mIdleTime = 50;
            _mMaxCallbackThreads = 100;
            _mVerifyConnectionInterval = 100;
            _mEncoding = Encoding.UTF8;

            _sem = new SemaphoreSlim(0);
            _waiting = false;

            _activeThreads = 0;
        }

        public int Port
        {
            get
            {
                return _mPort;
            }
            set
            {
                if (value < 0)
                {
                    return;
                }

                if (_mPort == value)
                {
                    return;
                }

                if (_mIsOpen)
                {
                    throw new Exception("Invalid attempt to change port while still open.\nPlease close port before changing.");
                }

                _mPort = value;
                if (_listener == null)
                {
                    //this should only be called the first time.
                    //_listener = new TcpListener(IPAddress.Any, _mPort);
                    _listener = MakeListener();
                }
                else
                {
                    _listener.Server.Bind(new IPEndPoint(IPAddress.Any, _mPort));
                }
            }
        }

        public int MaxSendAttempts
        {
            get
            {
                return _mMaxSendAttempts;
            }
            set
            {
                _mMaxSendAttempts = value;
            }
        }

        [Browsable(false)]
        public bool IsOpen
        {
            get
            {
                return _mIsOpen;
            }
            set
            {
                if (_mIsOpen == value)
                {
                    return;
                }

                if (value)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        public List<TcpServerConnection> Connections
        {
            get
            {
                List<TcpServerConnection> rv = new List<TcpServerConnection>();
                rv.AddRange(_connections);
                return rv;
            }
        }

        public int IdleTime
        {
            get
            {
                return _mIdleTime;
            }
            set
            {
                _mIdleTime = value;
            }
        }

        public int MaxCallbackThreads
        {
            get
            {
                return _mMaxCallbackThreads;
            }
            set
            {
                _mMaxCallbackThreads = value;
            }
        }

        public int VerifyConnectionInterval
        {
            get
            {
                return _mVerifyConnectionInterval;
            }
            set
            {
                _mVerifyConnectionInterval = value;
            }
        }

        public Encoding Encoding
        {
            get
            {
                return _mEncoding;
            }
            set
            {
                Encoding oldEncoding = _mEncoding;
                _mEncoding = value;
                foreach (TcpServerConnection client in _connections)
                {
                    if (client.Encoding == oldEncoding)
                    {
                        client.Encoding = _mEncoding;
                    }
                }
            }
        }

        public void SetEncoding(Encoding encoding, bool changeAllClients)
        {
            Encoding oldEncoding = _mEncoding;
            _mEncoding = encoding;
            if (changeAllClients)
            {
                foreach (TcpServerConnection client in _connections)
                {
                    client.Encoding = _mEncoding;
                }
            }
        }

        private void RunListener()
        {
            while (_mIsOpen && _mPort >= 0)
            {
                try
                {
                    if (_listener.Pending())
                    {
                        TcpClient socket = _listener.AcceptTcpClient();
                        TcpServerConnection conn = new TcpServerConnection(socket, _mEncoding);

                        if (OnConnect != null)
                        {
                            lock (_activeThreadsLock)
                            {
                                _activeThreads++;
                            }
                            conn.CallbackThread = new Thread(() =>
                            {
                                OnConnect(conn);
                            });
                            conn.CallbackThread.Start();
                        }

                        lock (_connections)
                        {
                            _connections.Add(conn);
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(_mIdleTime);
                    }
                }
                catch (ThreadInterruptedException) { } //thread is interrupted when we quit
                catch (Exception e)
                {
                    if (_mIsOpen && OnError != null)
                    {
                        OnError(this, e);
                    }
                }
            }
        }

        private void RunSender()
        {
            while (_mIsOpen && _mPort >= 0)
            {
                try
                {
                    bool moreWork = false;
                    for (int i = 0; i < _connections.Count; i++)
                    {
                        if (_connections[i].CallbackThread != null)
                        {
                            try
                            {
                                _connections[i].CallbackThread = null;
                                lock (_activeThreadsLock)
                                {
                                    _activeThreads--;
                                }
                            }
                            catch (Exception)
                            {
                                //an exception is thrown when setting thread and old thread hasn't terminated
                                //we don't need to handle the exception, it just prevents decrementing activeThreads
                            }
                        }

                        if (_connections[i].CallbackThread != null) { }
                        else if (_connections[i].Connected() &&
                            (_connections[i].LastVerifyTime.AddMilliseconds(_mVerifyConnectionInterval) > DateTime.UtcNow ||
                             _connections[i].VerifyConnected()))
                        {
                            moreWork = moreWork || ProcessConnection(_connections[i]);
                        }
                        else
                        {
                            lock (_connections)
                            {
                                _connections.RemoveAt(i);
                                i--;
                            }
                        }
                    }

                    if (!moreWork)
                    {
                        System.Threading.Thread.Yield();
                        lock (_sem)
                        {
                            foreach (TcpServerConnection conn in _connections)
                            {
                                if (conn.HasMoreWork())
                                {
                                    moreWork = true;
                                    break;
                                }
                            }
                        }
                        if (!moreWork)
                        {
                            _waiting = true;
                            _sem.Wait(_mIdleTime);
                            _waiting = false;
                        }
                    }
                }
                catch (ThreadInterruptedException) { } //thread is interrupted when we quit
                catch (Exception e)
                {
                    if (_mIsOpen && OnError != null)
                    {
                        OnError(this, e);
                    }
                }
            }
        }

        private bool ProcessConnection(TcpServerConnection conn)
        {
            bool moreWork = false;
            if (conn.ProcessOutgoing(_mMaxSendAttempts))
            {
                moreWork = true;
            }

            if (OnDataAvailable != null && _activeThreads < _mMaxCallbackThreads && conn.Socket.Available > 0)
            {
                lock (_activeThreadsLock)
                {
                    _activeThreads++;
                }
                conn.CallbackThread = new Thread(() =>
                {
                    OnDataAvailable(conn);
                });
                conn.CallbackThread.Start();
                Thread.Yield();
            }
            return moreWork;
        }

        public void Open()
        {
            lock (this)
            {
                if (_mIsOpen)
                {
                    //already open, no work to do
                    return;
                }
                if (_mPort < 0)
                {
                    throw new Exception("Invalid port");
                }

                try
                {
                    _listener.Start();
                }
                catch (Exception)
                {
                    _listener.Stop();
                    _listener = MakeListener();
                    _listener.Start();
                }

                _mIsOpen = true;

                _listenThread = new Thread(new ThreadStart(RunListener));
                _listenThread.Start();

                _sendThread = new Thread(new ThreadStart(RunSender));
                _sendThread.Start();
            }
        }

        public void Close()
        {
            if (!_mIsOpen)
            {
                return;
            }

            lock (this)
            {
                _mIsOpen = false;
                foreach (TcpServerConnection conn in _connections)
                {
                    conn.ForceDisconnect();
                }
                try
                {
                    if (_listenThread.IsAlive)
                    {
                        _listenThread.Interrupt();

                        Thread.Yield();
                        if (_listenThread.IsAlive)
                        {
                            _listenThread.Abort();
                        }
                    }
                }
                catch (System.Security.SecurityException) { }
                try
                {
                    if (_sendThread.IsAlive)
                    {
                        _sendThread.Interrupt();

                        Thread.Yield();
                        if (_sendThread.IsAlive)
                        {
                            _sendThread.Abort();
                        }
                    }
                }
                catch (System.Security.SecurityException) { }
            }
            _listener.Stop();

            lock (_connections)
            {
                _connections.Clear();
            }

            _listenThread = null;
            _sendThread = null;
            GC.Collect();
        }

        public void Send(string data)
        {
            lock (_sem)
            {
                if (_connections.Count == 0)
                {
                    if (OnError != null)
                    {
                        OnError(this, new Exception("No connections are established."));
                    }
                }

                foreach (TcpServerConnection conn in _connections)
                {
                    conn.SendData(data);
                }
                Thread.Yield();
                if (_waiting)
                {
                    _sem.Release();
                    _waiting = false;
                }
            }
        }

        private TcpListener MakeListener()
        {
            _listener = new TcpListener(IPAddress.Any, _mPort);
            //string hostName = Dns.GetHostName();
            //string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString(); 
            //IPEndPoint ep = new IPEndPoint(IPAddress.Parse(myIP), _mPort);
            //_listener = new TcpListener(ep);
            return _listener;
        }
    }
}
