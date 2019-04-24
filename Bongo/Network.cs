using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bongo {
	class Network {

		// Server:
		byte _nextPlayer = 2;
		private Socket _serverSocket;
		private List<Socket> _connectedSockets = new List<Socket>();
		private const int BufferSize = 2048;
		private static byte[] _buffer = new byte[BufferSize];
		private bool _serverRunning = false;

		// Client:
		private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		// https://www.youtube.com/watch?v=xgLRe7QV6QI

		// Misc:
		List<Player> _players = new List<Player>();
		byte _playerId = 1;

		public event EventHandler<SystemMessageEventArgs> OnSystemMessage;
		public event EventHandler OnConnectedToServer;
		public event EventHandler OnServerShutdown;
		public event EventHandler<BingoBoardEventArgs> OnReceivedBingoBoard;
		public event EventHandler<PlayerListEventArgs> OnPlayerListUpdated;

		const string ServerStarting = "Starting server... ";
		const string ServerStarted = "Server started ";
		const string ClientConnected = "Client connected ";
		const string ClientIdSent = "Client {0} joined";
		const string ClientDisconnectForce = "Client disconnected forcefully ";
		const string ConnectionAttempt = "Connection attempt {0}... ";
		const string ConnectedToServer = "Connected to server ";

		const byte PrefixIndex = 2;
		const byte PlayerIdIndex = 3;
		const byte ContentStartIndex = 4;

		enum BufferPrefixes {
			None,
			FullBoard,
			Chat,
			ConnectionId,
			PlayerJoined,
			PlayerLeft,
			Name,
			Color,
			PlayerList,
			Confirm,
		}

		public bool Connected {
			get {
				return _clientSocket.Connected || _serverRunning; }
			set {
			}
		}

		#region disconnection

		public void Disconnect() {
			if (_serverRunning) {
				CloseAllSockets();
			}
			else if (_clientSocket.Connected) {
				SendPlayerLeft();
				ClientDisconnect();
			}
		}

		public void ClientDisconnect() {
			_clientSocket.Shutdown(SocketShutdown.Both);
			_clientSocket.Close();
			_players = new List<Player>();
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Disconnected"));
			OnServerShutdown.Invoke(this, new EventArgs());
		}

		#endregion

		#region server

		/// <summary>
		/// Starts the server
		/// </summary>
		/// <param name="port"></param>
		public void StartServer(int port) {
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ServerStarting));
			_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
			_serverSocket.Listen(0);
			_serverSocket.BeginAccept(AcceptCallBack, null);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ServerStarted));
			_serverRunning = true;

			Player serverPlayer = new Player();
			serverPlayer.Id = _playerId;
			_players.Add(serverPlayer);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}


		/// <summary>
		/// Stops the server
		/// </summary>
		private void CloseAllSockets() {
			SendPlayerLeft(_playerId);
			for (int i = _players.Count - 1; i > 0; i--) {
				_players[i].Socket.Shutdown(SocketShutdown.Both);
				_players[i].Socket.Close();
			}
			_players = new List<Player>();
			//_serverSocket.Shutdown(SocketShutdown.Both);
			_serverSocket.Close();
			_serverRunning = false;
			_nextPlayer = 2;
		}

		/// <summary>
		/// Server receives information from connected client
		/// </summary>
		/// <param name="IA"></param>
		private void ReceiveCallBack(IAsyncResult IA) {
			Socket current = (Socket)IA.AsyncState;
			int received;

			try {
				received = current.EndReceive(IA);
			}
			catch (SocketException) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ClientDisconnectForce));
				current.Close();
				Player player = _players.Find(p => p.Socket == current);
				SendPlayerLeft(player.Id);
				_players.Remove(player);
				return;
			}
			catch (ObjectDisposedException) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("ObjectDisposedException"));
				return;
			}

			if (received == 0) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Received 0"));
				return;
			}
			ReceiveBuffer(_buffer, received);

			// relay message to other clients
			if (_buffer[PrefixIndex] != (byte)BufferPrefixes.Confirm) {
				byte[] receivedBuffer = new byte[received];
				Array.Copy(_buffer, receivedBuffer, received);
				ServerSendToEveryone(receivedBuffer);
			}
			try {
				current.BeginReceive(_buffer, 0, BufferSize, SocketFlags.None, ReceiveCallBack, current);
			}
			catch (ObjectDisposedException) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("ObjectDisposedException"));
			}
		}

		/// <summary>
		/// Receive connection from client
		/// </summary>
		/// <param name="IA"></param>
		private void AcceptCallBack(IAsyncResult IA) {
			Socket socket;

			try {
				socket = _serverSocket.EndAccept(IA);
			}
			catch (ObjectDisposedException) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("ObjectDisposedException"));
				return;
			}

			socket.BeginReceive(_buffer, 0, BufferSize, SocketFlags.None, ReceiveCallBack, socket);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ClientConnected));
			_serverSocket.BeginAccept(AcceptCallBack, null);


			Player player = new Player();
			player.Socket = socket;
			AssignPlayerId(player);
			_players.Add(player);
			SendPlayerJoined(player.Id);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		#endregion

		#region playerlist management

		/// <summary>
		/// Assign a player id to newly connected client
		/// </summary>
		/// <param name="player"></param>
		private void AssignPlayerId(Player player) {
			player.Id = _nextPlayer;
			_nextPlayer++;

			SendConnectionId(player.Socket, player.Id);
		}

		Player GetPlayer(byte[] buffer) {
			byte playerId = buffer[PlayerIdIndex];
			Player player = _players.Find(p => p.Id == playerId);
			if (player != null) {
				return player;
			}
			player = new Player();
			player.Id = playerId;
			_players.Add(player);
			return player;
		}

		Player GetPlayer(byte playerId) {
			Player player = _players.Find(p => p.Id == playerId);
			if (player != null) {
				return player;
			}
			player = new Player();
			player.Id = playerId;
			_players.Add(player);
			return player;
		}

		byte[] GetContent(byte[] buffer) {
			byte[] content = new byte[buffer.Length - ContentStartIndex];
			Array.Copy(buffer, ContentStartIndex, content, 0, content.Length);
			return content;
		}

		#endregion

		#region client

		/// <summary>
		/// Client receive bytes
		/// </summary>
		public void ReceiveResponse() {
			try {
				byte[] buffer = new byte[2048];
				int received = _clientSocket.Receive(buffer, SocketFlags.None);     // TODO: Socketexception if host stops
				if (received == 0) {
					OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Received 0"));
					return;
				}
				ReceiveBuffer(buffer, received);
			}
			catch (SocketException) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("SocketException"));
			}
		}

		/// <summary>
		/// Client connect to server
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		public void ConnectToServer(IPAddress address, int port) {
			_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			int attempts = 0;

			while (!_clientSocket.Connected) {
				try {
					attempts++;
					OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format(ConnectionAttempt, attempts)));
					_clientSocket.Connect(address, port);
					if (attempts > 10) {
						OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Failed to connect"));
						OnServerShutdown.Invoke(this, new EventArgs());
						return;
					}
				}
				catch (SocketException) {
					OnSystemMessage.Invoke(this, new SystemMessageEventArgs("SocketException"));
				}
			}
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ConnectedToServer));
			OnConnectedToServer.Invoke(this, new EventArgs());
		}

		#endregion

		#region received transmissions

		private void ProcessReceivedBuffer(byte[] buffer) {
			Player player = GetPlayer(buffer);
			byte[] content = GetContent(buffer);

			switch (buffer[PrefixIndex]) {
				case (byte)BufferPrefixes.Chat:
					ReceiveChat(player, content);
					break;
				case (byte)BufferPrefixes.FullBoard:
					ReceiveBingoBoard(content);
					break;
				case (byte)BufferPrefixes.ConnectionId:
					ReceiveConnectionId(content);
					break;
				case (byte)BufferPrefixes.PlayerJoined:
					ReceivePlayerJoined(content);
					break;
				case (byte)BufferPrefixes.PlayerLeft:
					ReceivePlayerLeft(content);
					break;
				case (byte)BufferPrefixes.Name:
					ReceiveName(player, content);
					break;
				case (byte)BufferPrefixes.Color:
					ReceiveColor(player, content);
					break;
				case (byte)BufferPrefixes.PlayerList:
					ReceivePlayerList(content);
					break;
				case (byte)BufferPrefixes.Confirm:
					ReceiveConfirm(player);
					break;
				default:
					break;
			}
		}

		private void ReceiveBuffer(byte[] buffer, int received) {
			int newBufferStartIndex = 0;
			for (int i = 1; i < received - 1; i++) {
				if (buffer[i] != 0 || buffer[i + 1] != 0 || buffer[i + 2] == 0) {
					continue;
				}
				byte[] bufferW = new byte[i];
				Array.Copy(buffer, newBufferStartIndex, bufferW, 0, i);
				ProcessReceivedBuffer(bufferW);
				newBufferStartIndex = i;
			}
			byte[] bufferWF = new byte[received - newBufferStartIndex];
			Array.Copy(buffer, newBufferStartIndex, bufferWF, 0, bufferWF.Length);
			ProcessReceivedBuffer(bufferWF);
		}

		#endregion

		#region send transmissions general

		byte[] MakeByteArray(BufferPrefixes prefix, byte[] content) {
			byte[] buffer = new byte[content.Length + ContentStartIndex];
			buffer[0] = 0;
			buffer[1] = 0;
			buffer[PrefixIndex] = (byte)prefix;
			buffer[PlayerIdIndex] = _playerId;
			Array.Copy(content, 0, buffer, ContentStartIndex, content.Length);
			return buffer;
		}

		void SendToEveryone(byte[] buffer) {
			// send the bytes
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			// if server, send message to everyone
			else {
				ServerSendToEveryone(buffer);
			}
		}

		public void ServerSendToEveryone(byte[] buffer) {
			foreach (Player player in _players) {
				if (player.Id > _playerId) {
					try {
						player.Socket.Send(buffer);
					}
					catch (ObjectDisposedException) {
						OnSystemMessage.Invoke(this, new SystemMessageEventArgs("ObjectDisposedException"));
						continue;
					}
				}
			}
		}

		#endregion

		#region transmission types

		/// <summary>
		/// Transmits a chat message
		/// </summary>
		/// <param name="message"></param>
		public void SendChat(string message) {
			//// convert the typed text to bytes, and add a byte in front of it to denote it is text
			byte[] buffer = MakeByteArray(BufferPrefixes.Chat, Encoding.ASCII.GetBytes(message));
			ReceiveChat(GetPlayer(_playerId), message);
			SendToEveryone(buffer);
		}

		private void ReceiveChat(Player sender, byte[] content) {
			string message = Encoding.ASCII.GetString(content);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("{0}: {1}", sender.Name, message)));
		}

		private void ReceiveChat(Player sender, string message) {
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("{0}: {1}", sender.Name, message)));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Sends the entire bingo board
		/// </summary>
		/// <param name="colors"></param>
		public void SendBingoBoard(int[] colors) {
			// convert array of colors to byte with one byte to denote that it is a list of colors
			byte[] colorBytes = new byte[colors.Length];
			for (int i = 0; i < 25; i++) {
				Array.Copy(BitConverter.GetBytes(colors[i]), 0, colorBytes, i * 4, 4);
			}
			byte[] buffer = MakeByteArray(BufferPrefixes.FullBoard, colorBytes);
			SendToEveryone(buffer);
		}

		private void ReceiveBingoBoard(byte[] content) {
			int[] colorsInt = new int[25];
			for (int i = 0; i < 25; i++) {
				colorsInt[i] = BitConverter.ToInt32(content, i * 4);
			}
			OnReceivedBingoBoard(this, new BingoBoardEventArgs(colorsInt));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Sends a player id to someone who just joined
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="id"></param>
		private void SendConnectionId(Socket socket, byte id) {
			byte[] buffer = MakeByteArray(BufferPrefixes.ConnectionId, new byte[] { id });
			socket.Send(buffer);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format(ClientIdSent, id)));
		}

		private void ReceiveConnectionId(byte[] content) {
			_playerId = content[0];
			SendConfirm();
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("You are player {0}", _playerId)));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		// Todo: Also include names and colors in this
		private void SendPlayerList(Player to) {
			byte[] buffersPlayerList = new byte[_players.Count];
			List<byte> PlayersListList = new List<byte>();
			foreach (Player player in _players) {
				PlayersListList.Add(player.Id);
				PlayersListList.Add(player.Color);
				byte[] name = Encoding.ASCII.GetBytes(player.Name);
				PlayersListList.AddRange(name);
				PlayersListList.Add(byte.MaxValue);
			}
			buffersPlayerList = PlayersListList.ToArray();
			byte[] buffer = MakeByteArray(BufferPrefixes.PlayerList, buffersPlayerList);
			to.Socket.Send(buffer);
		}

		private void ReceivePlayerList(byte[] content) {
			int currentPlayerStartingIndex = 0;
			for (int i = 0; i < content.Length; i++) {
				if (content[i] != byte.MaxValue) {
					continue;
				}
				Player player = GetPlayer(content[currentPlayerStartingIndex]);
				player.Color = content[currentPlayerStartingIndex + 1];
				player.Name = Encoding.ASCII.GetString(content, currentPlayerStartingIndex + 2, i - currentPlayerStartingIndex - 2);
				currentPlayerStartingIndex = i + 1;
			}
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		private void SendPlayerJoined(byte id) {
			byte[] buffer = MakeByteArray(BufferPrefixes.PlayerJoined, new byte[] { id });
			ReceivePlayerJoined(id);
			ServerSendToEveryone(buffer);
		}

		private void ReceivePlayerJoined(byte[] content) {
			GetPlayer(content[0]);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceivePlayerJoined(byte id) {
			GetPlayer(id);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		private void SendPlayerLeft() {
			byte[] buffer = MakeByteArray(BufferPrefixes.PlayerLeft, new byte[] { _playerId });
			SendToEveryone(buffer);
		}

		private void SendPlayerLeft(byte id) {
			byte[] buffer = MakeByteArray(BufferPrefixes.PlayerLeft, new byte[] { id });
			ReceivePlayerLeft(id);
			ServerSendToEveryone(buffer);
		}

		private void ReceivePlayerLeft(byte[] content) {
			Player player = GetPlayer(content[0]);
			if (player == _players[0] && _clientSocket.Connected == true) {
				ClientDisconnect();
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Host shut down the server"));
			}
			// if i have socket information (ie im the host), close sockets
			if (player.Socket != null) {
				player.Socket.Shutdown(SocketShutdown.Both);
				player.Socket.Close();
			}
			_players.Remove(player);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceivePlayerLeft(byte id) {
			Player player = GetPlayer(id);
			// if player left is the host, disconnect
			if (player == _players[0] && _clientSocket.Connected == true) {
				ClientDisconnect();
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Host shut down the server"));
			}
			// if i have socket information (ie im the host), close sockets
			if (player.Socket != null) {
				player.Socket.Shutdown(SocketShutdown.Both);
				player.Socket.Close();
			}
			_players.Remove(player);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}


		// --------------------------------------------------------------------------------------------------------------------------

		public void SendName(string name) {
			byte[] buffer = MakeByteArray(BufferPrefixes.Name, Encoding.ASCII.GetBytes(name));
			ReceiveName(GetPlayer(_playerId), name);
			SendToEveryone(buffer);
		}

		private void ReceiveName(Player player, byte[] content) {
			string name = Encoding.ASCII.GetString(content);
			player.Name = string.Empty;
			player.Name = name;
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceiveName(Player player, string name) {
			player.Name = string.Empty;
			player.Name = name;
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		public void SendColor(byte color) {
			byte[] buffer = MakeByteArray(BufferPrefixes.Color, new byte[] { color });
			ReceiveColor(GetPlayer(_playerId), color);
			SendToEveryone(buffer);
		}

		private void ReceiveColor(Player player, byte[] content) {
			player.Color = content[0];
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceiveColor(Player player, byte color) {
			player.Color = color;
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		// --------------------------------------------------------------------------------------------------------------------------

		private void SendConfirm() {
			byte[] buffer = MakeByteArray(BufferPrefixes.Confirm, new byte[] { _playerId });
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
		}

		private void ReceiveConfirm(Player player) {
			if (!_clientSocket.Connected) {
				SendPlayerList(player);
			}
		}

		#endregion
	}
}
