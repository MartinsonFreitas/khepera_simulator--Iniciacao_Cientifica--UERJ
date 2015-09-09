﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Controller
{
    public static class ConnectionExtensions
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {
            var connection = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections()
                .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return connection != null ? connection.State : TcpState.Unknown;
        }
    }

    enum ConnectionResult
    {
        IncorrectParameters,
        ServerUnavailable,
        IncorrectRobotId,
        IncorrectAccessCode,
        Connected
    }

    class ConnectionManager
    {
        private const int SERVER_PORT_NUMBER = 6020;
        private const int TYPE_ID_CONTROLLER = 0;

        private TcpClient _tcpClient;

        public ConnectionManager()
        {
            _tcpClient = new TcpClient();
        }

        public ConnectionResult Connect(string hostname, ushort entityId, string accessCode, int miliSecTimeout)
        {
            ConnectionResult result = ConnectionResult.ServerUnavailable;
            try
            {
                _tcpClient.ConnectAsync(hostname, SERVER_PORT_NUMBER).Wait(miliSecTimeout);
            }
            catch(Exception)
            {
                result = ConnectionResult.ServerUnavailable;
            }
            if (_tcpClient.Connected)
            {
                _tcpClient.GetStream().WriteByte(TYPE_ID_CONTROLLER);
                BinaryWriter writer = new BinaryWriter(_tcpClient.GetStream());
                writer.Write((ushort)IPAddress.HostToNetworkOrder((short)entityId));
                System.Threading.Thread.Sleep(200);
                if (_tcpClient.GetState() == TcpState.Established)
                {
                    //writer.Write(accessCode.Length);
                    result = ConnectionResult.Connected;
                }
                else
                    result = ConnectionResult.IncorrectRobotId;
            }
            if (result != ConnectionResult.Connected)
                Disconnect();
            return result;
        }

        public void Disconnect()
        {
            _tcpClient.Close();
            _tcpClient = new TcpClient();
        }
    }
}