using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PewPew.Server
{
    class Communicator
    {
        private const int MAX_CONCURRENT_CONNECTIONS = 1;
        private const int HANDSHAKE_LENGTH = 255;
        private const int SERVER_PORT = 8080;
        private const string SERVER_ADDRESS = "127.0.0.1";

        private Socket _socketServer;
        private IPEndPoint _endPoint;
        private Socket _socketClient;

        public enum States
        {
            Uninitialized,
            Initialized,
            BeginListening,
            WaitingForClient,
            ClientAccepted,
            Handshaking,
            Closed,
            Error
        }

        public Dictionary<States, string> dicStates = new Dictionary<States, string>()
        {
            {States.Uninitialized, "Uninitialized"}, {States.Initialized, "Initialized"},
            {States.BeginListening, "Begin listening"}, {States.WaitingForClient, "Waiting for client"}, 
            {States.ClientAccepted, "Client accepted"}, {States.Handshaking, "Handshaking"},
            {States.Closed, "Closed"}, {States.Error, "Error"}
        };

        public States _state = States.Uninitialized;

        public Communicator()
        {
            Initialize();
        }

        public void BeginListening()
        {
            _socketServer.Bind(_endPoint);
            _socketServer.Listen(MAX_CONCURRENT_CONNECTIONS);
            _state = States.BeginListening;
        }

        public void WaitForConnection()
        {
            _state = States.WaitingForClient;
            var e = new SocketAsyncEventArgs();
            e.Completed += AcceptCallback;
            if (!_socketServer.AcceptAsync(e))
            {
                AcceptCallback(_socketServer, e);
            }
        }

        public void Close()
        {
            try
            {
                if (_socketClient != null)
                {
                    _socketClient.Close();
                }
                _socketServer.Close();
                _state = States.Closed;
                Initialize();
            }
            catch (Exception)
            {
                _state = States.Error;
            }
        }

        public string GetState()
        {
            return dicStates[_state];
        }

        private void AcceptCallback(object sender, SocketAsyncEventArgs e)
        {
            Socket listenSocket = (Socket)sender;
            try
            {
                do
                {
                    try
                    {
                        _socketClient = e.AcceptSocket;
                        Debug.Assert(_socketClient != null);

                        Handshake();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        e.AcceptSocket = null; // to enable reuse
                    }
                } while (!listenSocket.AcceptAsync(e));
            }
            catch (Exception)
            {
                _state = States.Error;
            }
        }
        private void Initialize()
        {
            _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _endPoint = new IPEndPoint(IPAddress.Parse(SERVER_ADDRESS), SERVER_PORT); // TOOD: magic
            _state = States.Initialized;
        }

        private void Handshake()
        {
            if (_socketClient.Connected)
            {
                _state = States.Handshaking;
                var networkStream = new NetworkStream(_socketClient);
                var streamWriter = new StreamWriter(networkStream);
                var streamReader = new StreamReader(networkStream);
                
                var shake = new char[HANDSHAKE_LENGTH];
                streamReader.Read(shake, 0, HANDSHAKE_LENGTH);

                var handshake = "HTTP/1.1 101 Switching Protocols\r\n" +
                                "Upgrade: websocket\r\n" +
                                "Connection: Upgrade\r\n" +
                                "Sec-WebSocket-Accept: HSmrc0sMlYUkAGmm5OPpG2HaGWk=\r\n" +
                                "Sec-WebSocket-Protocol: chat";
                
                streamWriter.Write(handshake);
                streamWriter.Flush();
            }
        }
    }
}
