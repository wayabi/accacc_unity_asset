using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Nito;

namespace AccAcc
{
    class AccAccTcpServer
    {
        private int size_receiving_;
        private Socket handler_;
        public object lock_;
        public Deque<byte> queue_;
        public delegate void OnDisconnect(string s);
        public delegate void OnConnect();
        public delegate void OnListenError(string s);
        public delegate void OnListenStart(int port);
        OnDisconnect on_disconnect_;
        OnConnect on_connect_;
        OnListenError on_listen_error_;
        OnListenStart on_listen_start_;
        Socket listener_;

        // State object for reading client data asynchronously
        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 102400;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        List<StateObject> activeConnections = new List<StateObject>();

        public void StartListening(String host, int port, OnConnect on_connect, OnDisconnect on_disconnect, OnListenError on_listen_error, OnListenStart on_listen_start)
        {
            on_connect_ = on_connect;
            on_disconnect_ = on_disconnect;
            on_listen_error_ = on_listen_error;
            on_listen_start_ = on_listen_start;
            size_receiving_ = 0;
            lock_ = new object();
            queue_ = new Deque<byte>(1000000);
            IPAddress ipAddress = IPAddress.Parse(GetIPAddress(host));

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            listener_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener_.Bind(localEndPoint);
                listener_.Listen(0);

                // Start an asynchronous socket to listen for connections.
                listener_.BeginAccept(new AsyncCallback(AcceptCallback), listener_);
                on_listen_start_(port);

            }
            catch (Exception e)
            {
                //on_disconnect_(e.ToString());
                on_listen_error_(e.ToString());
            }

        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            handler_ = handler;

            on_connect_();

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);

            //確立した接続のオブジェクトをリストに追加
            activeConnections.Add(state);

            System.Console.WriteLine("there is {0} connections", activeConnections.Count);

            //1listener. AccAccTcpServer connect to only one clinet.
            //listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                //state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                // Check for end-of-file tag. If it is not there, read 
                // more data.
                lock (lock_)
                {
                    for (int i = 0; i < bytesRead; ++i)
                    {
                        queue_.AddToBack(state.buffer[i]);
                    }
                }

                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            else
            {
                on_disconnect_("stop receiving");
                if (activeConnections.Contains(state))
                {
                    activeConnections.Remove(state);
                }

                listener_.BeginAccept(new AsyncCallback(AcceptCallback), listener_);
            }
        }

        public void Send(String data)
        {
            if (handler_ == null) return;

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler_.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler_);

        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private string GetIPAddress(string hostname)
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(hostname);

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }

        public void stop()
        {
            //一応。なくてもいける？
            if (listener_ != null)
            {
                foreach (StateObject so in activeConnections)
                {
                    so.workSocket.Close();
                }
                listener_.BeginDisconnect(true, null, null);
                listener_.Close();
            }
        }
    }
}
