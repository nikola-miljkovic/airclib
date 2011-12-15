/* IrcClient.cs
 * 
 * Advanced IRC Library Project
 * Copyright (C) 2011 Nikola Miljkovic <http://code.google.com/p/airclib/>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using airclib.Base;
using airclib.Constants;
using airclib.StringReader;

namespace airclib
{
    public delegate void ConnectionChangedEventHandler(object sender, IrcConnectionEventArgs e);
    public delegate void OnDataSentEventHandler(object sender, IrcDataEventArgs e);
    public delegate void OnReciveDataEventHandler(object sender, IrcDataEventArgs e);
    public delegate void OnChannelJoinEventHandler(object sender, IrcChannelEventArgs channel);
    public delegate void OnUserJoinedChannelEventHandler(object sender, IrcChannelEventArgs e);
    public delegate void OnUserLeftChannelEventHandler(object sender, IrcChannelEventArgs e);

    /// <summary>
    /// IrcClient class, main class of airclib.
    /// </summary>
    public class IrcClient
    {
        #region Events and variables

        //Variables
        private readonly TcpClient _connection = new TcpClient();
        private readonly IrcSReader _stringReader = new IrcSReader();
        private Thread _listenThread;

        private bool _isConnected;
        private string _nick;

        //Events
        public event ConnectionChangedEventHandler OnConnectionChanged;
        public event OnDataSentEventHandler OnDataSent;
        public event OnReciveDataEventHandler OnReciveData;
        public event OnChannelJoinEventHandler OnChannelJoin;
        public event OnChannelJoinEventHandler OnChannelPart;
        public event OnUserJoinedChannelEventHandler OnUserJoinedChannel;
        public event OnUserLeftChannelEventHandler OnUserLeftChannel;

        #endregion

        #region Class Constructors

        /// <summary>
        /// Initializes IrcClient class.
        /// </summary>
        /// <param name="server">Server that IrcClient will connect to.</param>
        /// <param name="port">Port of server.</param>
        public IrcClient(string server, int port)
        {
            ChannelCount = 0;
            ServerAdress = server;
            ServerPort = port;
        }

        /// <summary>
        /// Initializes IrcClient class.
        /// </summary>
        /// <param name="server">Server that IrcClient will connect to.</param>
        public IrcClient(IrcServer server)
            : this(server.Server, server.Port)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets, sets current connections server port.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Gets, sets current connections server adress.
        /// </summary>
        public string ServerAdress { get; set; }

        /// <summary>
        /// Gets current connections server count based on how much channels client is in.
        /// </summary>
        public uint ChannelCount { get; set; }

        #endregion

        #region Connection

        /// <summary>
        /// Connects to irc server.
        /// </summary>
        public void Connect()
        {
            try
            {
                _connection.Connect(ServerAdress, ServerPort);
            }
            catch (Exception exc)
            {
                throw exc;
            }

            Stream = _connection.GetStream();
            _isConnected = true;

            if (OnConnectionChanged != null)
            {
                var args = new IrcConnectionEventArgs()
                               {
                                   ServerAdress = this.ServerAdress,
                                   Port = this.ServerPort,
                                   Connected = true
                               };
                OnConnectionChanged(this, args);
            }
        }

        /// <summary>
        /// Connection event, connects to irc server.
        /// </summary>
        /// <param name="server">Server address or ip.</param>
        /// <param name="port">Server port.</param>
        public void Connect(string server, int port)
        {
            try
            {
                _connection.Connect(server, port);
            }
            catch (Exception exc)
            {
                throw exc;
            }

            Stream = _connection.GetStream();
            _isConnected = true;

            if (OnConnectionChanged != null)
            {
                var args = new IrcConnectionEventArgs()
                               {
                                   ServerAdress = server,
                                   Port = port,
                                   Connected = true
                               };
                OnConnectionChanged(this, args);
            }
        }

        /// <summary>
        /// Connection event, connects to wanted "IrcServer"
        /// </summary>
        /// <param name="server">Server structure.</param>
        public void Connect(IrcServer server)
        {
            Connect(server.Server, server.Port);
        }

        /// <summary>
        /// Sends data to connected server, must be connected to some server.
        /// </summary>
        /// <param name="data"></param>
        public void SendData(string data)
        {
            try
            {
                using (var writer = new StreamWriter(Stream))
                {
                    writer.WriteLine(data);
                    writer.Flush();

                    if (OnDataSent != null)
                    {
                        var args = new IrcDataEventArgs()
                                       {
                                           Data = data,
                                           Sender = _stringReader.ReadNick(data)
                                       };
                        OnDataSent(this, args);
                    }

                }
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        /// <summary>
        /// Reads all incoming data from socket.
        /// </summary>
        /// <param name="listen">If set true, it will start listening.</param>
        public void Listen(bool listen)
        {
            if (!listen && _listenThread.IsAlive)
                _listenThread.Abort();

            try
            {
                _listenThread = new Thread(KeepListen);
                _listenThread.Start();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        /// <summary>
        /// Disconnects from server.
        /// </summary>
        public void Disconnect()
        {
            _connection.Close();
            Listen(false);
            _isConnected = false;
            ChannelCount = 0;

            var args = new IrcConnectionEventArgs()
                           {
                               ServerAdress = this.ServerAdress,
                               Port = this.ServerPort,
                               Connected = false
                           };
            OnConnectionChanged(this, args);
        }

        #endregion

        #region Connection States

        /// <summary>
        /// Bool, returns isConnected.
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return _isConnected;
        }

        /// <summary>
        /// Returns channel count.
        /// </summary>
        /// <returns></returns>
        public bool IsInChannel()
        {
            return ChannelCount != 0;
        }

        /// <summary>
        /// Returns connections current irc Nick.
        /// </summary>
        /// <returns>String</returns>
        public string GetNick()
        {
            return _nick;
        }

        /// <summary>
        /// Returns clients current stearm.
        /// </summary>
        /// <returns></returns>
        public NetworkStream Stream 
        { 
            private set;
            get;
        }

        #endregion

        #region IRC Commands

        /// <summary>
        /// Quits, disconnects from server, with leaving message.
        /// </summary>
        /// <param name="message">Leaving message.</param>
        public void Quit(string message)
        {
            SendData("QUIT :" + message);
            _connection.Close();
        }

        /// <summary>
        /// Joins channel.
        /// </summary>
        /// <param name="channel">Channel name.</param>
        public void JoinChannel(string channel)
        {
            if (!_isConnected)
                return;

            SendData("JOIN " + channel);
        }

        /// <summary>
        /// Leaves channel.
        /// </summary>
        /// <param name="channel">Channel.</param>
        public void LeaveChannel(string channel)
        {
            if (!_isConnected || ChannelCount == 0)
                return;

            SendData("PART " + channel);
            ChannelCount--;
        }

        /// <summary>
        /// Requests topic channel.
        /// </summary>
        /// <param name="Channel">Channel.</param>
        public void GetTopic(string dhannel)
        {
            if (!_isConnected)
                return;

            SendData("TOPIC " + dhannel);
        }

        /// <summary>
        /// Gets name list from channel.
        /// </summary>
        /// <param name="channel">From channel.</param>
        public void GetNames(string channel)
        {
            if (!_isConnected || ChannelCount == 0)
                return;

            SendData("NAMES " + channel);
        }

        /// <summary>
        /// Query, messages nick.
        /// </summary>
        /// <param name="targetNick">Sends message to this Nick/User.</param>
        /// <param name="message">Message.</param>
        /// <param name="color">Wanted message color.</param>
        public void MessageUser(string targetNick, string message, ColorMessages color = ColorMessages.Black)
        {
            if (!_isConnected)
                return;

            var data = String.Format("PRIVMSG {0} :\u0003{2} {1}", targetNick, message, (int) color);
            SendData(data);
        }

        /// <summary>
        /// Sends message to wanted channel, connection must be connected to channel. With color.
        /// </summary>
        /// <param name="channel">Channel name, connection must be connected to this channel.</param>
        /// <param name="message">Message.</param>
        /// <param name="color">Wanted color.</param>
        public void MessageChannel(string channel, string message, ColorMessages color = ColorMessages.Black)
        {
            if (!_isConnected)
                return;

            var data = String.Format("PRIVMSG {0} :\u0003{1} {2}", channel, (int) color, message);
            SendData(data);
        }

        /// <summary>
        /// Does wanted action.
        /// </summary>
        /// <param name="target">Target channel or user.</param>
        /// <param name="ction">Action text.</param>
        public void DoAction(string target, string action)
        {
            if (!_isConnected)
                return;

            var data = String.Format("PRIVMSG {0} :ACTION {1}", target, action);
            SendData(data);
        }

        /// <summary>
        /// Request change of current connection's nick name.
        /// </summary>
        /// <param name="nick">Wanted nick.</param>
        public void SetNick(string nick)
        {
            if (!_isConnected)
                return;

            SendData("NICK " + nick); // TO-DO: Confirm from server...
            _nick = nick;
        }

        /// <summary>
        /// Request who is of user.
        /// </summary>
        /// <param name="nick">Users nick.</param>
        public void WhoIs(string nick)
        {
            if (!_isConnected)
                return;

            SendData("WHOIS " + nick);
        }

        /// <summary>
        /// Sends notice message to user.
        /// </summary>
        /// <param name="user">Users nickname.</param>
        /// <param name="message">Notice.</param>
        public void Notice(string user, string message)
        {
            if (!_isConnected)
                return;

            var data = String.Format("NOTICE {0} :{1}", user, message);
            SendData(data);
        }

        /// <summary>
        /// Requests server's current motd, message of the day.
        /// </summary>
        public void ServerMOTD()
        {
            if (!_isConnected)
                return;

            SendData("MOTD");
        }

        /// <summary>
        /// Invites user with wanted nickname to wanted channel.
        /// </summary>
        /// <param name="nickname">Users nickname.</param>
        /// <param name="channel">Wanted channel.</param>
        public void InviteToChannel(string nickname, string channel)
        {
            if (!_isConnected)
                return;

            var data = String.Format("INVITE {0} {1}", nickname, channel);
            SendData(data);
        }

        /// <summary>
        /// Sets away, and away message.
        /// </summary>
        /// <param name="away">Boolean, true for being away.</param>
        /// <param name="message">Message, only works if Away is true.</param>
        public void SetAway(bool away, string message)
        {
            if (!_isConnected)
                return;

            if (!away)
                SendData("AWAY");
            else
                SendData("AWAY " + message);
        }

        /// <summary>
        /// Sets user info. Args talk for them self.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="hostName"></param>
        /// <param name="serverName"></param>
        /// <param name="realName"></param>
        public void SetUserInfo(string userName, string hostName, string serverName, string realName)
        {
            // Command: USER
            // Parameters: <username> <hostname> <servername> <realname>
            if (!_isConnected)
                return;

            string data = String.Format("USER {0} {1} {2} {3}", userName, hostName, serverName, realName);
            SendData(data);
        }

        #endregion

        /// <summary>
        /// Main listener, threaded.
        /// </summary>
        private void KeepListen()
        {
            using (var reader = new StreamReader(Stream))
            {
                while (Stream.DataAvailable)
                {
                    var data = reader.ReadLine(); 
                    // it cannot be null?
                    if (String.IsNullOrEmpty(data) == true)
                        break;

                    if (data.StartsWith("PING"))
                    {
                        SendData(data.Replace("PING", "PONG"));
                    }
                    else if (_stringReader.ReadCommand(data) == "JOIN")
                    {
                        var splitData = data.Split(' ');
                        splitData[0] = _stringReader.ReadNick(splitData[0]);
                        if (OnUserJoinedChannel != null && splitData[0] != _nick)
                        {
                            var args = new IrcChannelEventArgs()
                                        {
                                            Channel = splitData[2],
                                            Nick = splitData[0]
                                        };
                            OnUserJoinedChannel(this, args);
                        }
                        if (OnChannelJoin != null && splitData[0] == _nick)
                        {
                            ChannelCount++;
                            var args = new IrcChannelEventArgs()
                                        {
                                            Channel = splitData[2],
                                            Nick = _nick
                                        };
                            OnChannelJoin(this, args);
                        }
                    }
                    else if (_stringReader.ReadCommand(data) == "PART")
                    {
                        var splitData = data.Split(' ');
                        splitData[0] = _stringReader.ReadNick(splitData[0]);
                        if (OnUserLeftChannel != null && splitData[0] != _nick)
                        {
                            var args = new IrcChannelEventArgs()
                                        {
                                            Channel = splitData[2],
                                            Nick = splitData[0]
                                        };
                            OnUserLeftChannel(this, args);
                        }
                        if (OnChannelPart != null && splitData[0] == _nick)
                        {
                            ChannelCount--;
                            var args = new IrcChannelEventArgs()
                                        {
                                            Channel = splitData[2],
                                            Nick = _nick
                                        };
                            OnChannelPart(this, args);
                        }
                    }

                    if (OnReciveData != null)
                    {
                        var args = new IrcDataEventArgs()
                                       {
                                           Data = data,
                                           Sender = _stringReader.ReadSender(data)
                                       };
                        OnReciveData(this, args);
                    }
                }
            }
        }

        /// <summary>
        /// Gets singe Irc channel.
        /// </summary>
        /// <param name="channel">Chanel name.</param>
        /// <returns>Irc channel.</returns>
        public Channel GetChannel(string channel)
        {
            return new Channel(channel, this);
        }
    }
}


