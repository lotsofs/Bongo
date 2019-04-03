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
		byte _nextPlayer = 1;
		private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		private List<Socket> _connectedSockets = new List<Socket>();
		private const int BufferSize = 2048;
		private static byte[] _buffer = new byte[BufferSize];
		private bool _serverRunning = false;

		// Client:
		private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		// https://www.youtube.com/watch?v=xgLRe7QV6QI

		// Misc:
		List<Player> _players = new List<Player>();
		byte _playerId = 0;

		public event EventHandler<SystemMessageEventArgs> OnSystemMessage;
		public event EventHandler OnConnectedToServer;
		public event EventHandler<BingoBoardEventArgs> OnReceivedBingoBoard;
		public event EventHandler<PlayerListEventArgs> OnPlayerListUpdated;

		const string ServerStarting = "Starting server... ";
		const string ServerStarted = "Server started ";
		const string ClientConnected = "Client connected ";
		const string ClientIdSent = "Client {0} joined";
		const string ClientDisconnectForce = "Client disconnected forcefully ";
		const string ConnectionAttempt = "Connection attempt {0}... ";
		const string ConnectedToServer = "Connected to server ";

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
		
		#region server

		/// <summary>
		/// Starts the server
		/// </summary>
		/// <param name="port"></param>
		public void StartServer(int port) {
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ServerStarting));
			_serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
			_serverSocket.Listen(0);
			_serverSocket.BeginAccept(AcceptCallBack, null);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ServerStarted));
			_serverRunning = true;

			Player serverPlayer = new Player();
			serverPlayer.Id = 0;
			_players.Add(serverPlayer);
		}


		/// <summary>
		/// Stops the server
		/// </summary>
		private void CloseAllSockets() {
			for (int i = _players.Count - 1; i >= 0; i--) {
				_players[i].Socket.Shutdown(SocketShutdown.Both);
				_players[i].Socket.Close();
				_players.RemoveAt(i);
			}
			_serverSocket.Shutdown(SocketShutdown.Both);
			_serverSocket.Close();
			_serverRunning = false;
			_nextPlayer = 1;
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

			ReceiveBuffer(_buffer, received);

			// relay message to other clients
			if (_buffer[0] != (byte)BufferPrefixes.Confirm) {
				byte[] receivedBuffer = new byte[received];
				Array.Copy(_buffer, receivedBuffer, received);
				SendBytesToEveryone(receivedBuffer);
			}
			current.BeginReceive(_buffer, 0, BufferSize, SocketFlags.None, ReceiveCallBack, current);
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
			SendPlayerJoined(player.Id);
			_players.Add(player);
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

		private Player GetPlayer(byte id) {
			Player player = _players.Find(p => p.Id == id);
			if (player != null) {
				return player;
			}
			player = new Player();
			Debug.WriteLine("Added player: " + id);
			player.Id = id;
			_players.Add(player);
			return player;
		}

		#endregion

		#region client

		/// <summary>
		/// Client receive bytes
		/// </summary>
		public void ReceiveResponse() {
			byte[] buffer = new byte[2048];
			int received = _clientSocket.Receive(buffer, SocketFlags.None);		// TODO: Socketexception if host stops
			if (received == 0) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Received 0"));
				return;
			}
			ReceiveBuffer(buffer, received);
		}

		/// <summary>
		/// Client connect to server
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		public void ConnectToServer(IPAddress address, int port) {
			int attempts = 0;

			while (!_clientSocket.Connected) {
				try {
					attempts++;
					OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format(ConnectionAttempt, attempts)));
					_clientSocket.Connect(address, port);
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

		private void ReceiveBuffer(byte[] buffer, int received) {
			byte[] b = new byte[received];
			Array.Copy(buffer, b, received);
			switch (buffer[0]) {
				case (byte)BufferPrefixes.Chat:
					ReceiveChat(b);
					break;
				case (byte)BufferPrefixes.FullBoard:
					ReceiveBoard(b);
					break;
				case (byte)BufferPrefixes.ConnectionId:
					ReceiveConnectionId(b);
					break;
				case (byte)BufferPrefixes.PlayerJoined:
					ReceivePlayerJoined(b);
					break;
				case (byte)BufferPrefixes.PlayerLeft:
					ReceivePlayerLeft(b);
					break;
				case (byte)BufferPrefixes.Name:
					ReceiveName(b);
					break;
				case (byte)BufferPrefixes.Color:
					ReceiveColor(b);
					break;
				case (byte)BufferPrefixes.PlayerList:
					ReceivePlayerList(b);
					break;
				case (byte)BufferPrefixes.Confirm:
					ReceiveConfirm(b);
					break;
				default:
					break;
			}
		}

		private void ReceiveChat(byte[] receiveBuffer) {
			string text = Encoding.ASCII.GetString(receiveBuffer);
			text = text.Substring(2);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("Chat message: {0}", text)));
		}

		private void ReceiveBoard(byte[] receiveBuffer) {
			int[] colorsInt = new int[25];
			for (int i = 0; i < 25; i++) {
				colorsInt[i] = BitConverter.ToInt32(receiveBuffer, i * 4 + 2);
			}
			OnReceivedBingoBoard(this, new BingoBoardEventArgs(colorsInt));
		}

		private void ReceiveConnectionId(byte[] receiveBuffer) {
			_playerId = receiveBuffer[1];
			SendConfirm();
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("You are player {0}", _playerId)));
		}

		private void ReceivePlayerList(byte[] receiveBuffer) {
			for (int i = 1; i < receiveBuffer.Length; i++) {
				GetPlayer(receiveBuffer[i]);
			}
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceivePlayerJoined(byte[] receiveBuffer) {
			GetPlayer(receiveBuffer[1]);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceivePlayerLeft(byte[] receiveBuffer) {
			Player player = GetPlayer(receiveBuffer[1]);
			_players.Remove(player);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceiveName(byte[] receiveBuffer) {
			string name = Encoding.ASCII.GetString(receiveBuffer);
			name = name.Substring(2);
			Player player = GetPlayer(receiveBuffer[1]);
			player.Name = string.Empty;
			player.Name = name;
			Debug.WriteLine(name.Length);
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceiveColor(byte[] receiveBuffer) {
			Player player = GetPlayer(receiveBuffer[1]);
			player.Color = receiveBuffer[2];
			OnPlayerListUpdated.Invoke(this, new PlayerListEventArgs(_players));
		}

		private void ReceiveConfirm(byte[] receiveBuffer) {
			Player player = GetPlayer(receiveBuffer[1]);
			if (!_clientSocket.Connected) {
				SendPlayerList(player); 
			}
		}

		#endregion

		#region send transmissions

		public void SendBytesToEveryone(byte[] buffer) {
			foreach (Player player in _players) {
				if (player.Id > 0) {
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

		/// <summary>
		/// Transmits a chat message
		/// </summary>
		/// <param name="message"></param>
		public void SendTextMessage(string message) {
			// convert the typed text to bytes, and add a byte in front of it to denote it is text
			byte[] bufferWorking = Encoding.ASCII.GetBytes(message);
			byte[] buffer = new byte[bufferWorking.Length + 2];
			buffer[0] = (byte)BufferPrefixes.Chat;
			buffer[1] = _playerId;
			Array.Copy(bufferWorking, 0, buffer, 2, bufferWorking.Length);

			// send the bytes
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			// if server, send message to everyone
			else {
				ReceiveChat(buffer);
				SendBytesToEveryone(buffer);
			}
		}

		/// <summary>
		/// Sends the entire bingo board
		/// </summary>
		/// <param name="colors"></param>
		public void SendBingoBoard(int[] colors) {
			// convert array of colors to byte with one byte to denote that it is a list of colors
			byte[] buffer = new byte[colors.Length * 4 + 2];
			for (int i = 0; i < 25; i++) {
				Array.Copy(BitConverter.GetBytes(colors[i]), 0, buffer, i * 4 + 2, 4);
			}
			buffer[0] = (byte)BufferPrefixes.FullBoard;
			buffer[1] = _playerId;

			// transmit data
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			else {
				ReceiveBoard(buffer);
				SendBytesToEveryone(buffer);
			}
		}

		/// <summary>
		/// Sends a player id to someone who just joined
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="id"></param>
		private void SendConnectionId(Socket socket, byte id) {
			byte[] buffer = new byte[2];
			buffer[0] = (byte)BufferPrefixes.ConnectionId;
			buffer[1] = id;
			socket.Send(buffer);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format(ClientIdSent, id)));
		}

		// Todo: Also include names and colors in this
		private void SendPlayerList(Player player) {
			byte[] buffer = new byte[_players.Count + 1];
			buffer[0] = (byte)BufferPrefixes.PlayerList;
			for (int i = 0; i < _players.Count; i++) {
				buffer[i + 1] = _players[i].Id;
			}
			player.Socket.Send(buffer);
		}

		private void SendPlayerJoined(byte id, Socket socket = null) {
			byte[] buffer = new byte[2];
			buffer[0] = (byte)BufferPrefixes.PlayerJoined;
			buffer[1] = id;
			if (socket != null) {
				socket.Send(buffer);
				return;
			}
			SendBytesToEveryone(buffer);
		}

		private void SendPlayerLeft(byte id) {
			byte[] buffer = new byte[2];
			buffer[0] = (byte)BufferPrefixes.PlayerLeft;
			buffer[1] = id;
			SendBytesToEveryone(buffer);
		}

		public void SendName(string name) {
			byte[] bufferWorking = Encoding.ASCII.GetBytes(name);
			byte[] buffer = new byte[bufferWorking.Length + 2];
			buffer[0] = (byte)BufferPrefixes.Name;
			buffer[1] = _playerId;
			Array.Copy(bufferWorking, 0, buffer, 2, bufferWorking.Length);
			// send the bytes
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			// if server, send message to everyone (including self)
			else {
				ReceiveName(buffer);
				SendBytesToEveryone(buffer);
			}
		}

		private void SendName(string name, byte id, Socket socket) {
			byte[] bufferWorking = Encoding.ASCII.GetBytes(name);
			byte[] buffer = new byte[bufferWorking.Length + 2];
			buffer[0] = (byte)BufferPrefixes.Name;
			buffer[1] = id;
			Array.Copy(bufferWorking, 0, buffer, 2, bufferWorking.Length);
			socket.Send(buffer);
		}

		public void SendColor(byte color) {
			byte[] buffer = new byte[3];
			buffer[0] = (byte)BufferPrefixes.Color;
			buffer[1] = _playerId;
			buffer[2] = color;

			// send the bytes
			if (_clientSocket.Connected) {
				//Thread.Sleep(1);
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			// if server, send message to everyone
			else {
				ReceiveColor(buffer);
				SendBytesToEveryone(buffer);
			}
		}

		private void SendColor(byte color, byte id, Socket socket) {
			byte[] buffer = new byte[3];
			buffer[0] = (byte)BufferPrefixes.Color;
			buffer[1] = id;
			buffer[2] = color;
			//Thread.Sleep(1);
			socket.Send(buffer);
		}

		private void SendConfirm() {
			byte[] buffer = new byte[2];
			buffer[0] = (byte)BufferPrefixes.Confirm;
			buffer[1] = _playerId;
			if (_clientSocket.Connected) {
				//Thread.Sleep(1);
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
		}

		#endregion
	}
}
