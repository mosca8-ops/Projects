using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    [AddComponentMenu("WEAVR/Remote Control/Channels/TCP Socket Channel")]
    public class TcpSocketChannel : MonoBehaviour, ICommandChannel
    {
		#region [  PRIVATE MEMBERS  ]
		/// <summary>
		/// The port number to fallback to when unable to retrieve from config file
		/// </summary>
		[SerializeField]
		[Tooltip("The port number to use in case there is no port found in config file")]
		private int m_fallbackPort = 9000;
		/// <summary>
		/// The buffer size to use to stream data. This is a fallback value
		/// </summary>
		[SerializeField]
		[Tooltip("The buffer size to use to stream data")]
		private int m_bufferSize = 2048;
		/// <summary>
		/// Whether to debug the incoming client messages or not
		/// </summary>
		[SerializeField]
		[Tooltip("Whether to debug client messages or not")]
		private bool m_debugMessages = true;

		/// <summary>
		/// The event is raised when a new client has been connected
		/// </summary>
		[Space]
		[SerializeField]
		private UnityEventString m_onClientConnected;

		/// <summary>
		/// The event is raised when a new client has been disconnected
		/// </summary>
		[Space]
		[SerializeField]
		private UnityEventString m_onClientDisconnected;

		/// <summary>
		/// The actual port number to use for the connection
		/// </summary>
		private int m_actualPortNumber;

		/// <summary>
		/// The actual buffer size to stream data
		/// </summary>
		private int m_actualBufferSize;

		/// <summary> 	
		/// TCPListener to listen for incomming TCP connection 	
		/// requests. 	
		/// </summary> 	
		private TcpListener m_tcpListener;

		/// <summary> 
		/// Background thread for TcpServer workload. 	
		/// </summary> 	
		private Thread m_tcpListenerThread;

		/// <summary> 	
		/// Create handle to connected tcp client. 	
		/// </summary> 	
		private TcpClient m_connectedTcpClient;

		/// <summary>
		/// The clients messages
		/// </summary>
		private Dictionary<TcpClient, ClientHandler> m_clientHandlers;

		/// <summary>
		/// Whether the exchange operations can be performed or not
		/// </summary>
		private bool m_exchangeIsLocked;

		/// <summary>
		/// The client wrappers to be added in Update loop
		/// </summary>
		private List<ClientHandler> m_clientHandlersToAdd;

		/// <summary>
		/// The clients to be removed in Update loop
		/// </summary>
		private List<TcpClient> m_clientsToRemove;

		/// <summary>
		/// The pool to handle requests to avoid GC spikes
		/// </summary>
		private List<TcpRequest> m_requestsPool;

		#endregion

		/// <summary>
		/// Event to be raised when a new request arrives
		/// </summary>
		public event OnRequestDelegate OnNewRequest;

		/// <summary>
		/// Event to be raised when a new Client has been connected
		/// </summary>
		public event OnDataPointEvent DataPointOpened;

		/// <summary>
		/// Event to be raised when a Client has been disconnected
		/// </summary>
		public event OnDataPointEvent DataPointClosed;

		/// <summary>
		/// Class container for messages to send or retrieve from the client
		/// </summary>
		private class ClientHandler : IDataInterfacePoint
		{
			/// <summary>
			/// Associated client
			/// </summary>
			public TcpClient client;
			/// <summary>
			/// Running thread for this client
			/// </summary>
			public Thread thread;

			public readonly ConcurrentQueue<RequestData> receivedMessages = new ConcurrentQueue<RequestData>();
			public readonly ConcurrentQueue<RequestData> sendMessages = new ConcurrentQueue<RequestData>();
			/// <summary>
			/// Whether this client is ready to receive/send data
			/// </summary>
			public bool IsReady => client.Connected;

			/// <summary>
			/// This event is raised when a new message has been received from the client
			/// </summary>
			public event BytesReceivedDelegate OnReceivedBytes;
			public event InterfacePointReadyDelegate OnReady;

			public ClientHandler(TcpClient client)
			{
				this.client = client;
				if (client.Connected)
				{
					OnReady?.Invoke(this);
				}
			}

			/// <summary>
			/// Sends the message in bytes to the client
			/// </summary>
			/// <param name="bytes">The bytes to be sent</param>
			public void SendBytes(byte[] bytes)
			{
				try
				{
					client.GetStream().Write(bytes, 0, bytes.Length);
				}
				catch (Exception e)
				{
					WeavrDebug.LogException(this, e);
				}
			}
		}

		/// <summary>
		/// The class to handle request and response to and from the same client
		/// </summary>
		private class TcpRequest : IRequest
		{
			/// <summary>
			/// The client to respond to
			/// </summary>
			public ClientHandler clientHandler;

			/// <summary>
			/// The delegate to send the message to the client
			/// </summary>
			public Action<TcpClient, RequestData> sendCallback;

			/// <summary>
			/// The delegate to be called once the response has been sent
			/// </summary>
			public Action<TcpRequest> onResponseSent;

			/// <summary>
			/// The request from the client
			/// </summary>
			public RequestData request;

			/// <summary>
			/// Gets the source of this request
			/// </summary>
			public IDataInterfacePoint Source => clientHandler;

			/// <summary>
			/// Gets the concrete request
			/// </summary>
			/// <returns>The request data</returns>
			public RequestData GetRequest() => request;

			/// <summary>
			/// Send the response back
			/// </summary>
			/// <param name="response">The response to be sent back</param>
			public void SendResponse(in RequestData response)
			{
				sendCallback(clientHandler.client, response);
				onResponseSent(this);
			}
		}

		private void Awake()
		{
			InitializeValues();
		}

		private void InitializeValues()
		{
			m_clientHandlers = new Dictionary<TcpClient, ClientHandler>();
			m_requestsPool = new List<TcpRequest>();
		}

		void OnEnable()
		{
			WeavrRemoteControl.Register(this);
			// TODO: get it from config
			m_actualPortNumber = m_fallbackPort;
			m_actualBufferSize = m_bufferSize;
			StartTcpServer();
		}
		
		void OnDisable()
		{
			StopTcpServer();
			WeavrRemoteControl.Unregister(this);
		}

		private void Update()
		{
			SpinClientsMessages();
		}

		private void SpinClientsMessages()
		{
			if (m_exchangeIsLocked) { return; }

			// Check if new clients arrived
			if (m_clientHandlersToAdd?.Count > 0)
			{
				var clientsToAdd = m_clientHandlersToAdd;
				// Release immediately the resource
				m_clientHandlersToAdd = null;

				// Add the new clients
				foreach (var clientHandler in clientsToAdd)
				{
					m_clientHandlers[clientHandler.client] = clientHandler;
					m_onClientConnected?.Invoke(clientHandler.client.Client.RemoteEndPoint?.ToString());
					DataPointOpened?.Invoke(clientHandler);
				}
			}

			// Fetch the previous unprocessed requests
			foreach (var pair in m_clientHandlers)
			{
				if (pair.Value.receivedMessages.TryDequeue(out RequestData message))
				{
					OnNewRequest?.Invoke(CreateRequest(pair.Value, message));
				}
			}

			// Check if there are clients to remove (i.e. disconnected clients)
			if (m_clientsToRemove?.Count > 0)
			{
				var clientsToRemove = m_clientsToRemove;
				// Remove immediately the resource
				m_clientsToRemove = null;
				// Remove the clients now
				foreach (var client in clientsToRemove)
				{
					if(m_clientHandlers.TryGetValue(client, out ClientHandler handler))
					{
						m_clientHandlers.Remove(client);
					} 
					m_onClientDisconnected?.Invoke(client?.Client?.RemoteEndPoint?.ToString());
				}
			}

		}

		private IRequest CreateRequest(ClientHandler clientHandler, RequestData request)
		{
			if(m_requestsPool.Count > 0)
			{
				var tcpRequest = m_requestsPool[m_requestsPool.Count - 1];
				m_requestsPool.RemoveAt(m_requestsPool.Count - 1);
				tcpRequest.clientHandler = clientHandler;
				tcpRequest.request = request;
				return tcpRequest;
			}
			return new TcpRequest()
			{
				clientHandler = clientHandler,
				request = request,
				sendCallback = SendMessageInternal,
				onResponseSent = ReclaimRequest,
			};
		}

		private void ReclaimRequest(TcpRequest request)
		{
			m_requestsPool.Add(request);
		}

		private void StartTcpServer()
		{
			// Start the background thread 		
			m_tcpListenerThread = new Thread(ListenForIncommingRequests);
			m_tcpListenerThread.IsBackground = true;
			m_tcpListenerThread.Start();
		}

		private void StopTcpServer()
		{
			// Stop the background thread
			try
			{
				m_tcpListenerThread?.Abort();
				// Stop the listener
				m_tcpListener?.Stop();
				// And close the current client if any
				if (m_connectedTcpClient?.Connected == true)
				{
					m_connectedTcpClient.Close();
					m_connectedTcpClient.Dispose();
				}
			}
			catch(Exception e)
			{
				WeavrDebug.LogException(this, e);
			}
		}

		/// <summary> 	
		/// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
		/// </summary> 	
		private void ListenForIncommingRequests()
		{
			try
			{
				// Create listener on defined port. 			
				m_tcpListener = new TcpListener(IPAddress.Any, m_actualPortNumber);
				m_tcpListener.Start();
				WeavrDebug.Log(this, $"Server is listening at: {m_tcpListener.LocalEndpoint}");
				while (true)
				{
					var connectedTcpClient = m_tcpListener.AcceptTcpClient();
					WeavrDebug.Log(this, $"Accepted client with address: {connectedTcpClient.Client.RemoteEndPoint}");

					m_exchangeIsLocked = true;
					if(!m_clientHandlers.TryGetValue(connectedTcpClient, out ClientHandler clientMessages))
					{
						clientMessages = new ClientHandler(connectedTcpClient);
						if(m_clientHandlersToAdd == null)
						{
							m_clientHandlersToAdd = new List<ClientHandler>() { clientMessages };
						}
						else
						{
							m_clientHandlersToAdd.Add(clientMessages);
						}
					}
					m_exchangeIsLocked = false;

					Thread clientThread = new Thread(() => HandleClient(connectedTcpClient, clientMessages));
					clientMessages.thread = clientThread;
					clientThread.Start();
					
					Thread.Sleep(1);
				}
			}
			catch (SocketException socketException)
			{
				WeavrDebug.LogError(this, "SocketException " + socketException.ToString());
			}
		}

		private void HandleClient(TcpClient client, ClientHandler messages)
		{
			using (client)
			{
				using (var stream = client.GetStream())
				{
					ProcessMessagesLoop(messages, stream);
				}
			}

			WeavrDebug.Log(this, "Client disconnected");

			m_exchangeIsLocked = true;
			if(m_clientsToRemove == null) { m_clientsToRemove = new List<TcpClient>() { client }; }
			else { m_clientsToRemove.Add(client); }
			m_exchangeIsLocked = false;
		}

		private void ProcessMessagesLoop(ClientHandler messages, NetworkStream stream)
		{
			byte[] fullBuffer = new byte[m_actualBufferSize * 4];
			byte[] bytes = new byte[m_actualBufferSize];
			int length;
			int offset = 0;
			// Read incomming stream into byte array. 						
			while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
			{
				// Check if not exceeding the size
				if(fullBuffer.Length < offset + length)
				{
					// If not enough size then double it!
					Array.Resize(ref fullBuffer, fullBuffer.Length * 2);
				}
				// Copy to full buffer
				Array.Copy(bytes, 0, fullBuffer, offset, length);
				offset += length;
				// Check if need to wait for additional part of the message or the message has been received
				if (length < m_bufferSize)
				{
					byte[] message = new byte[offset];
					Array.Copy(fullBuffer, 0, message, 0, offset);
					messages.receivedMessages.Enqueue(message);
					if (m_debugMessages)
					{
						DebugMessage(messages.client.Client.RemoteEndPoint.ToString(), message, offset);
					}
					offset = 0;
				}
			}
		}

		private void DebugMessage(string client, byte[] message, int length)
		{
			WeavrDebug.Log(this, $"Received from {client} [{length} bytes]: {Encoding.ASCII.GetString(message, 0, length)}");
		}

		private void SendMessageInternal(TcpClient client, RequestData message)
		{
			SendMessage(client, message.Bytes);
		}

		public void SendMessage(TcpClient client, string message)
		{
			SendMessage(client, Encoding.ASCII.GetBytes(message));
		}

		public void SendMessage(TcpClient client, byte[] byteArray)
		{
			try
			{
				client.GetStream().Write(byteArray, 0, byteArray.Length);
			}
			catch (Exception e)
			{
				WeavrDebug.LogException(this, e);
			}
		}

		public void BroadcastTcpMessage(string message)
		{
			var bytes = Encoding.ASCII.GetBytes(message);
			foreach (var pair in m_clientHandlers)
			{
				SendMessage(pair.Key, bytes);
			}
		}

		public void BroadcastResponse(in RequestData response) => BroadcastTcpMessage(response);
	}
}
