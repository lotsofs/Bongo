using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class Network {

		public event EventHandler<SystemMessageEventArgs> OnSystemMessage;
		public event EventHandler OnConnectedToServer;
		public event EventHandler<BingoBoardEventArgs> OnReceivedBingoBoard;

		// https://www.youtube.com/watch?v=xgLRe7QV6QI
		private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		private List<Socket> _connectedSockets = new List<Socket>();
		private const int BufferSize = 2048;
		//private const int Port;
		private static byte[] _buffer = new byte[BufferSize];
		private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		enum BufferPrefixes {
			None,
			FullBoard,
			Chat
		}

		const string ServerStarting = "Starting server... ";
		const string ServerStarted = "Server started ";
		const string ClientConnected = "Client connected ";
		const string ClientDisconnectForce = "Client disconnected forcefully ";
		const string ConnectionAttempt = "Connection attempt {0}... ";
		const string ConnectedToServer = "Connected to server ";

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
		}

		/// <summary>
		/// Stops the server
		/// </summary>
		private void CloseAllSockets() {
			foreach (Socket socket in _connectedSockets) {
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
			_serverSocket.Close();
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
				//return;
			}

			_connectedSockets.Add(socket);
			socket.BeginReceive(_buffer, 0, BufferSize, SocketFlags.None, ReceiveCallBack, socket);
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(ClientConnected));
			_serverSocket.BeginAccept(AcceptCallBack, null);
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
				_connectedSockets.Remove(current);
				return;
			}

			byte[] recBuf = new byte[received];

			switch (_buffer[0]) {
				case (byte)BufferPrefixes.Chat:
					Array.Copy(_buffer, recBuf, received);
					string text = Encoding.ASCII.GetString(recBuf);
					text = text.Substring(1);
					OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("Received: {0}", text)));
					break;
				case (byte)BufferPrefixes.FullBoard:
					Array.Copy(_buffer, recBuf, received);
					int[] colorsInt = new int[25];
					for (int i = 0; i < 25; i++) {
						colorsInt[i] = BitConverter.ToInt32(recBuf, i * 4 + 1);
					}
					OnReceivedBingoBoard(this, new BingoBoardEventArgs(colorsInt));
					break;
				default:
					break;
			}

			// relay message to other clients
			foreach (Socket socket in _connectedSockets) {
				if (socket != current) {
					socket.Send(recBuf);
				}

				current.BeginReceive(_buffer, 0, BufferSize, SocketFlags.None, ReceiveCallBack, current);
			}
		}

		/// <summary>
		/// Client receive bytes
		/// </summary>
		public void ReceiveResponse() {
			byte[] buffer = new byte[2048];
			int received = _clientSocket.Receive(buffer, SocketFlags.None);
			if (received == 0) {
				OnSystemMessage.Invoke(this, new SystemMessageEventArgs("Received 0"));
				return;
			}
			byte[] recBuf = new byte[received];
			Array.Copy(buffer, recBuf, received);

			switch (buffer[0]) {
				case (byte)BufferPrefixes.Chat:
					Array.Copy(buffer, recBuf, received);
					string text = Encoding.ASCII.GetString(recBuf);
					text = text.Substring(1);
					OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("Received: {0}", text)));
					break;
				case (byte)BufferPrefixes.FullBoard:
					Array.Copy (buffer, recBuf, received);
					int[] colorsInt = new int[25];
					for (int i = 0; i < 25; i++) {
						colorsInt[i] = BitConverter.ToInt32(recBuf, i * 4 + 1);
					}
					OnReceivedBingoBoard(this, new BingoBoardEventArgs(colorsInt));
					break;
				default:
					break;
			}
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

		/// <summary>
		/// Transmits a chat message
		/// </summary>
		/// <param name="message"></param>
		public void SendTextMessage(string message) {
			// convert the typed text to bytes, and add a byte in front of it to denote it is text
			byte[] bufferWorking = Encoding.ASCII.GetBytes(message);
			byte[] buffer = new byte[bufferWorking.Length + 1];
			buffer[0] = (byte)BufferPrefixes.Chat;
			Array.Copy(bufferWorking, 0, buffer, 1, bufferWorking.Length);
			
			// send the bytes
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			// if server, send message to everyone
			else {
				foreach (Socket socket in _connectedSockets) {
					socket.Send(buffer);
				}
			}
			OnSystemMessage.Invoke(this, new SystemMessageEventArgs(string.Format("Sent: {0}", message)));
		}

		/// <summary>
		/// Sends the entire bingo board
		/// </summary>
		/// <param name="colors"></param>
		public void SendBingoBoard(int[] colors) {
			// convert array of colors to byte with one byte to denote that it is a list of colors
			byte[] buffer = new byte[colors.Length * 4 + 1];
			for (int i = 0; i < 25; i++) {
				Array.Copy(BitConverter.GetBytes(colors[i]), 0, buffer, i * 4 + 1, 4);
			}
			buffer[0] = (byte)BufferPrefixes.FullBoard;

			// transmit data
			if (_clientSocket.Connected) {
				_clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
			}
			else {
				foreach (Socket socket in _connectedSockets) {
					socket.Send(buffer);
				}
			}
		}

		/// <summary>
		/// Returns whether the client is connected
		/// </summary>
		/// <returns></returns>
		public bool Connected() {
			return _clientSocket.Connected;
		}
	}
}
