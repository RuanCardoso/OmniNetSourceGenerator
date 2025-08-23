#pragma warning disable

using Omni.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemoryPack { }
namespace Newtonsoft.Json { }

public interface IMessage { }
public interface IMessageWithPeer { }


namespace UnityEngine
{
	public class Application
	{
		public static bool isPlaying { get; }
		public string persistentDataPath { get; }
	}
}

public class ObservableList<T> : List<T>
{
	public event Action<int, int> OnItemAdded;
	public event Action<int, int> OnItemRemoved;
	public event Action<int, int> OnItemUpdated;
	public Action<bool> OnUpdate; // true to send to players
}

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
	public event Action<int, int> OnItemAdded;
	public event Action<int, int> OnItemRemoved;
	public event Action<int, int> OnItemUpdated;
	public Action<bool> OnUpdate; // true to send to players
}

public enum Target
{
	/// <summary>
	/// Automatically selects the most appropriate recipients for the network message based on the current context.
	/// <para>
	/// On the server, this typically means broadcasting to all relevant clients. On the client, it may target the server or a specific group,
	/// depending on the operation being performed. This is the default and recommended option for most use cases.
	/// </para>
	/// </summary>
	Auto,

	/// <summary>
	/// Sends the message to all connected peers, including the sender itself.
	/// <para>
	/// Use this to broadcast updates or events that should be visible to every participant in the session, including the originator.
	/// </para>
	/// </summary>
	Everyone,

	/// <summary>
	/// Sends the message exclusively to the sender (the local peer).
	/// <para>
	/// This option is typically used for providing immediate feedback, confirmations, or updates that are only relevant to the sender and should not be visible to other peers.
	/// </para>
	/// <para>
	/// <b>Note:</b> If the sender is the server (peer id: 0), the message will be ignored and not processed. This ensures that server-only operations do not result in unnecessary or redundant network traffic.
	/// </para>
	/// </summary>
	Self,

	/// <summary>
	/// Sends the message to all connected peers except the sender.
	/// <para>
	/// Use this to broadcast information to all participants while excluding the originator, such as when relaying a player's action to others.
	/// </para>
	/// </summary>
	Others,

	/// <summary>
	/// Sends the message to all peers who are members of the same group(s) as the sender.
	/// <para>
	/// Sub-groups are not included. This is useful for group-based communication, such as team chat or localized events.
	/// </para>
	/// </summary>
	Group,

	/// <summary>
	/// Sends the message to all peers in the same group(s) as the sender, excluding the sender itself.
	/// <para>
	/// Sub-groups are not included. Use this to notify group members of an action performed by the sender, without echoing it back.
	/// </para>
	/// </summary>
	GroupOthers,
}

public class NetworkGroup
{

}

public class NetVarBehaviour : MonoBehaviour
{

	protected virtual void SyncNetworkState(NetworkPeer peer)
	{
	}

	protected void ___RegisterNetworkVariable___(string propertyName, byte propertyId, bool requiresOwnership,
			bool isClientAuthority, bool checkEquality, DeliveryMode deliveryMode, Target target, byte sequenceChannel)
	{
	}

	protected virtual void ___RegisterNetworkVariables___()
	{
	}
}

public class MonoBehaviour
{
}

public class DataBuffer : IDisposable
{

	public DataBuffer() { }

	public int Length { get; set; }
	public static DataBuffer Empty;

	public static DataBuffer Rent()
	{
		return default;
	}

	public void Write<T>(T value) { }

	public T Read<T>()
	{
		return default;
	}

	public T ReadAsBinary<T>()
	{
		return default;
	}

	public string ReadString()
	{
		return default;
	}

	public void WriteAsBinary<T>(T value) { }

	public T Deserialize<T>(NetworkPeer peer, bool IsServer)
	{
		return default;
	}

	public T Deserialize<T>()
	{
		return default;
	}

	public void CopyTo(DataBuffer buffer) { }

	public void RawWrite(Span<byte> data) { }

	public Span<byte> GetSpan()
	{
		return default;
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}

	public void SeekToBegin() { }
}
public class NetworkPeer { }

public class NetworkIdentity
{
	public T Get<T>()
			where T : class
	{
		return Get<T>(typeof(T).Name);
	}

	public T Get<T>(string serviceName)
			where T : class
	{
		return default;
	}
}

public class NetworkVariableOptions
{
	public static NetworkVariableOptions Default { get; set; } = new NetworkVariableOptions();
}

namespace Omni.Core
{
	public struct ClientOptions()
	{

	}

	public struct ServerOptions()
	{

	}

	public static class Service
	{
		/// <summary>
		/// Called when a service is added or updated, can be called multiple times.
		/// Be sure to unsubscribe to avoid double subscriptions. <br/><br/>
		/// - Subscribers should be called from the <c>OnAwake</c> method.<br/>
		/// - Unsubscribers should be called from the <c>OnStop</c> method.<br/>
		/// </summary>
		public static event Action<string> OnReferenceChanged;

		public static void UpdateReference(string componentName)
		{
			OnReferenceChanged?.Invoke(componentName);
		}
	}

	/// <summary>
	/// Service Locator is a pattern used to provide global access to a service instance.
	/// This class provides a static methods to store and retrieve services by name.
	/// </summary>
	public static partial class NetworkService
	{
		// (Service Name, Service Instance)
		private static readonly Dictionary<string, object> m_Services = new();

		/// <summary>
		/// Retrieves a service instance by its name from the service locator.
		/// Throws an exception if the service is not found or cannot be cast to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to which the service should be cast.</typeparam>
		/// <param name="serviceName">The name of the service to retrieve.</param>
		/// <returns>The service instance cast to the specified type.</returns>
		/// <exception cref="Exception">
		/// Thrown if the service is not found or cannot be cast to the specified type.
		/// </exception>
		public static T Get<T>(string serviceName)
			where T : class
		{
			try
			{
				if (m_Services.TryGetValue(serviceName, out object service))
				{
#if OMNI_RELEASE
                    return Unsafe.As<T>(service);
#else
					return (T)service;
#endif
				}
				else
				{
					throw new Exception(
						$"Could not find service with name: \"{serviceName}\" you must register the service before using it."
					);
				}
			}
			catch (InvalidCastException)
			{
				throw new Exception(
					$"Could not cast service with name: \"{serviceName}\" to type: \"{typeof(T)}\" check if you registered the service with the correct type."
				);
			}
		}

		/// <summary>
		/// Attempts to retrieve a service instance by its name from the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to retrieve.</typeparam>
		/// <param name="serviceName">The name of the service to retrieve.</param>
		/// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
		/// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
		public static bool TryGet<T>(string serviceName, out T service)
			where T : class
		{
			service = default;
			if (m_Services.TryGetValue(serviceName, out object @obj))
			{
				if (@obj is T)
				{
					service = Get<T>(serviceName);
					return true;
				}

				return false;
			}

			return false;
		}

		/// <summary>
		/// Retrieves a service instance by its type name from the service locator.
		/// </summary>
		/// <typeparam name="T">The type to which the service should be cast.</typeparam>
		/// <returns>The service instance cast to the specified type.</returns>
		/// <exception cref="Exception">
		/// Thrown if the service is not found or cannot be cast to the specified type.
		/// </exception>
		public static T Get<T>()
			where T : class
		{
			return Get<T>(typeof(T).Name);
		}

		/// <summary>
		/// Attempts to retrieve a service instance by its type from the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to retrieve.</typeparam>
		/// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
		/// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
		public static bool TryGet<T>(out T service)
			where T : class
		{
			service = default;
			string serviceName = typeof(T).Name;
			if (m_Services.TryGetValue(serviceName, out object @obj))
			{
				if (@obj is T)
				{
					service = Get<T>(serviceName);
					return true;
				}

				return false;
			}

			return false;
		}

		/// <summary>
		/// Adds a new service instance to the service locator with a specified name.
		/// Throws an exception if a service with the same name already exists.
		/// </summary>
		/// <typeparam name="T">The type of the service to add.</typeparam>
		/// <param name="service">The service instance to add.</param>
		/// <param name="serviceName">The name to associate with the service instance.</param>
		/// <exception cref="Exception">
		/// Thrown if a service with the specified name already exists.
		/// </exception>
		public static void Register<T>(T service, string serviceName)
		{
			if (!m_Services.TryAdd(serviceName, service))
			{
				throw new Exception(
					$"Could not add service with name: \"{serviceName}\" because it already exists."
				);
			}
		}

		/// <summary>
		/// Attempts to retrieve adds a new service instance to the service locator with a specified name.
		/// </summary>
		/// <typeparam name="T">The type of the service to add.</typeparam>
		/// <param name="service">The service instance to add.</param>
		/// <param name="serviceName">The name to associate with the service instance.</param>
		public static bool TryRegister<T>(T service, string serviceName)
		{
			return m_Services.TryAdd(serviceName, service);
		}

		/// <summary>
		/// Updates an existing service instance in the service locator with a specified name.
		/// Throws an exception if a service with the specified name does not exist.
		/// </summary>
		/// <typeparam name="T">The type of the service to update.</typeparam>
		/// <param name="service">The new service instance to associate with the specified name.</param>
		/// <param name="serviceName">The name associated with the service instance to update.</param>
		/// <exception cref="Exception">
		/// Thrown if a service with the specified name does not exist in the.
		/// </exception>
		public static void Update<T>(T service, string serviceName)
		{
			if (m_Services.ContainsKey(serviceName))
			{
				m_Services[serviceName] = service;
			}
			else
			{
				throw new Exception(
					$"Could not update service with name: \"{serviceName}\" because it does not exist."
				);
			}
		}

		/// <summary>
		/// Attempts to retrieve updates an existing service instance in the service locator with a specified name.
		/// </summary>
		/// <typeparam name="T">The type of the service to update.</typeparam>
		/// <param name="service">The new service instance to associate with the specified name.</param>
		/// <param name="serviceName">The name associated with the service instance to update.</param>
		public static bool TryUpdate<T>(T service, string serviceName)
		{
			if (m_Services.ContainsKey(serviceName))
			{
				m_Services[serviceName] = service;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Deletes a service instance from the service locator by its name.
		/// </summary>
		/// <param name="serviceName">The name of the service to delete.</param>
		/// <returns>True if the service was successfully removed; otherwise, false.</returns>
		public static bool Unregister(string serviceName)
		{
			return m_Services.Remove(serviceName);
		}

		/// <summary>
		/// Determines whether a service with the specified name exists in the service locator.
		/// </summary>
		/// <param name="serviceName"></param>
		/// <returns></returns>
		public static bool Exists(string serviceName)
		{
			return m_Services.ContainsKey(serviceName);
		}
	}
}

namespace Omni.Core
{
	public partial class NetworkManager
	{
		public static NetworkPeer SharedPeer;
		public static NetworkPeer LocalPeer;
		public static NetPool Pool;
		public static bool IsServerActive;
	}
}

public class NetPool()
{
	public DataBuffer Rent(bool enableTracking)
	{
		return default;
	}
}

namespace Omni.Core
{

	public class ServerBehaviour : NetVarBehaviour
	{
		protected virtual void Awake()
		{

		}

		protected virtual void Start() { }
		protected virtual void ___OnPropertyChanged___(
			string propertyName,
			byte propertyId,
			NetworkPeer peer,
			DataBuffer buffer
		)
		{ }

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Don't override this method! The source generator will override it.")]
		protected virtual void ___InjectServices___()
		{

		}


		protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name, byte id)
		{
			return false;
		}

		protected virtual void ___NotifyCollectionChange___() { }

		public class Event
		{
			public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }

			/// <summary>
			/// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="property">The property value to synchronize.</param>
			/// <param name="propertyId">The ID of the property being synchronized.</param>
			/// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
			public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
			{

			}
		}

		public StaticEventServer Server;
		public StaticEventClient Client;
	}

	public class ClientBehaviour : NetVarBehaviour
	{
		protected virtual void Awake()
		{

		}

		protected virtual void Start() { }
		protected virtual void ___OnPropertyChanged___(
			string propertyName,
			byte propertyId,
			NetworkPeer peer,
			DataBuffer buffer
		)
		{ }

		protected virtual void ___NotifyCollectionChange___() { }

		protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name, byte id)
		{
			return false;
		}

		public class Event
		{
			public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
		}

		public StaticEventServer Server;
		public StaticEventClient Client;
	}

	public class DualBehaviour : NetVarBehaviour
	{
		protected virtual void Awake()
		{

		}

		protected virtual void Start() { }
		protected virtual void ___OnPropertyChanged___(
			string propertyName,
			byte propertyId,
			NetworkPeer peer,
			DataBuffer buffer
		)
		{ }

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Don't override this method! The source generator will override it.")]
		protected virtual void ___InjectServices___()
		{

		}


		protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name, byte id)
		{
			return false;
		}

		protected virtual void ___NotifyCollectionChange___() { }

		public class Event
		{
			public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }

			/// <summary>
			/// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="property">The property value to synchronize.</param>
			/// <param name="propertyId">The ID of the property being synchronized.</param>
			/// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
			public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
			{

			}
		}

		public StaticEventServer Server;
		public StaticEventClient Client;
	}

	public class NetworkBehaviour : NetVarBehaviour
	{
		public bool IsMine => false;
		public bool IsServer => false;
		public bool IsClient => false;

		public NetworkIdentity Identity { get; set; }

		protected virtual void Awake()
		{

		}

		protected virtual void Start() { }

		protected virtual void ___OnPropertyChanged___(
			string propertyName,
			byte propertyId,
			NetworkPeer peer,
			DataBuffer buffer
		)
		{ }

		protected virtual void ___NotifyCollectionChange___() { }

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Don't override this method! The source generator will override it.")]
		protected virtual void ___InjectServices___()
		{

		}

		protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name, byte id)
		{
			return false;
		}

		public LocalEventServer Server;
		public LocalEventClient Client;
	}

	public class LocalEventClient
	{
		public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int channel = 0)
		{

		}

		public void Rpc<T1>(byte rpcId, T1 p1)
		{
		}

		public void Rpc<T1, T2>(byte rpcId, T1 p1, T2 p2)
		{
		}

		public void Rpc<T1, T2, T3>(byte rpcId, T1 p1, T2 p2, T3 p3)
		{
		}

		public void Rpc<T1, T2, T3, T4>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5, T6>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
		{
		}


		/// <summary>
		/// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		/// <param name="property">The property value to synchronize.</param>
		/// <param name="propertyId">The ID of the property being synchronized.</param>
		/// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
		public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
		{

		}
		public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }

		public void Invoke(byte msgId, ServerOptions options) { }
		public void Invoke(byte msgId, ClientOptions options) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, T1 p1, ClientOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, ClientOptions options = default)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, T1 p1, ServerOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, ServerOptions options = default)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}
	}

	public class LocalEventServer
	{
		public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, Target target = Target.Auto, int channel = 0, bool requiresOwnership = true)
		{

		}

		public void RpcViaPeer(byte rpcId, NetworkPeer peer, DataBuffer message = null, NetworkGroup group = null)
		{

		}

		public void Rpc(byte rpcId, DataBuffer buffer = null, NetworkGroup group = null)
		{
		}

		public void Rpc<T1>(byte rpcId, T1 p1, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2>(byte rpcId, T1 p1, T2 p2, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3>(byte rpcId, T1 p1, T2 p2, T3 p3, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3, T4>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5, T6>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, NetworkGroup group = null)
		{
		}


		/// <summary>
		/// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		/// <param name="property">The property value to synchronize.</param>
		/// <param name="propertyId">The ID of the property being synchronized.</param>
		/// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
		public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
		{

		}
		public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }

		public void Invoke(byte msgId, ServerOptions options) { }
		public void Invoke(byte msgId, ClientOptions options) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, T1 p1, ClientOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, ClientOptions options = default)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, T1 p1, ServerOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, ServerOptions options = default)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}
	}

	public class StaticEventServer
	{
		public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, Target target = Target.Auto, int channel = 0, bool requiresOwnership = true)
		{

		}

		public void Rpc(byte rpcId, NetworkPeer peer, DataBuffer buffer = null, NetworkGroup group = null)
		{
		}


		public void Rpc<T1>(byte rpcId, NetworkPeer peer, T1 p1, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2>(byte rpcId, NetworkPeer peer, T1 p1, T2 p2, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3>(byte rpcId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3, T4>(byte rpcId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5>(byte rpcId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, NetworkGroup group = null)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5, T6>(byte rpcId, NetworkPeer peer, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, NetworkGroup group = null)
		{
		}

		/// <summary>
		/// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		/// <param name="property">The property value to synchronize.</param>
		/// <param name="propertyId">The ID of the property being synchronized.</param>
		/// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
		public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
		{

		}
		public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
		public void Invoke(byte msgId, NetworkPeer peer, ServerOptions options) { }
		public void Invoke(byte msgId, ClientOptions options) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, NetworkPeer peer, T1 p1, ServerOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			T3 p3,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, T1 p1, ClientOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, ClientOptions options = default)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}
	}

	public class StaticEventClient
	{
		public void SetRpcParameters(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int channel = 0)
		{

		}

		public void Rpc<T1>(byte rpcId, T1 p1)
		{
		}

		public void Rpc<T1, T2>(byte rpcId, T1 p1, T2 p2)
		{
		}

		public void Rpc<T1, T2, T3>(byte rpcId, T1 p1, T2 p2, T3 p3)
		{
		}

		public void Rpc<T1, T2, T3, T4>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
		{
		}

		public void Rpc<T1, T2, T3, T4, T5, T6>(byte rpcId, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
		{
		}

		/// <summary>
		/// Sends a manual 'NetworkVariable' message to a specific client with the specified property and property ID.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		/// <param name="property">The property value to synchronize.</param>
		/// <param name="propertyId">The ID of the property being synchronized.</param>
		/// <param name="peer">The target client to receive the 'NetworkVariable' message.</param>
		public void NetworkVariableSyncToPeer<T>(T property, byte propertyId, NetworkPeer peer)
		{

		}
		public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
		public void Invoke(byte msgId, NetworkPeer peer, ServerOptions options) { }
		public void Invoke(byte msgId, ClientOptions options) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, NetworkPeer peer, T1 p1, ServerOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			T3 p3,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ServerOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, T1 p1, ClientOptions options = default)
				where T1 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, ClientOptions options = default)
			where T1 : unmanaged
			where T2 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
		{

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			T1 p1,
			T2 p2,
			T3 p3,
			T4 p4,
			T5 p5,
			ClientOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
			where T3 : unmanaged
			where T4 : unmanaged
			where T5 : unmanaged
		{

		}
	}
}

namespace Omni.Core
{
	public readonly struct Channel : IComparable, IComparable<Channel>, IComparable<int>, IEquatable<Channel>, IEquatable<int>, IConvertible, IFormattable
	{
		public static Channel MinValue => int.MinValue;
		public static Channel MaxValue => int.MaxValue;
		public static Channel Zero => 0;
		public static Channel One => 1;

		private readonly int value;

		public Channel(int value)
		{
			this.value = value;
		}

		public static implicit operator int(Channel d) => d.value;
		public static implicit operator Channel(int d) => new Channel(d);

		public bool Equals(Channel other) => value == other.value;
		public bool Equals(int other) => value == other;
		public override bool Equals(object obj) => obj is Channel mi ? value == mi.value : obj is int i && value == i;
		public override int GetHashCode() => value.GetHashCode();

		public static bool operator ==(Channel left, Channel right) => left.value == right.value;
		public static bool operator !=(Channel left, Channel right) => left.value != right.value;

		public int CompareTo(Channel other) => value.CompareTo(other.value);
		public int CompareTo(int other) => value.CompareTo(other);
		public int CompareTo(object obj)
		{
			if (obj is Channel mi) return value.CompareTo(mi.value);
			if (obj is int i) return value.CompareTo(i);
			throw new ArgumentException("Object is not a Channel or int");
		}

		public static bool operator <(Channel left, Channel right) => left.value < right.value;
		public static bool operator >(Channel left, Channel right) => left.value > right.value;
		public static bool operator <=(Channel left, Channel right) => left.value <= right.value;
		public static bool operator >=(Channel left, Channel right) => left.value >= right.value;

		public static Channel operator +(Channel a, Channel b) => new Channel(a.value + b.value);
		public static Channel operator -(Channel a, Channel b) => new Channel(a.value - b.value);
		public static Channel operator *(Channel a, Channel b) => new Channel(a.value * b.value);
		public static Channel operator /(Channel a, Channel b) => new Channel(a.value / b.value);
		public static Channel operator %(Channel a, Channel b) => new Channel(a.value % b.value);

		public static Channel operator -(Channel a) => new Channel(-a.value);
		public static Channel operator +(Channel a) => a;

		public static Channel operator &(Channel a, Channel b) => new Channel(a.value & b.value);
		public static Channel operator |(Channel a, Channel b) => new Channel(a.value | b.value);
		public static Channel operator ^(Channel a, Channel b) => new Channel(a.value ^ b.value);
		public static Channel operator ~(Channel a) => new Channel(~a.value);

		public static Channel operator <<(Channel a, int b) => new Channel(a.value << b);
		public static Channel operator >>(Channel a, int b) => new Channel(a.value >> b);

		public override string ToString() => value.ToString();
		public string ToString(string format, IFormatProvider formatProvider) => value.ToString(format, formatProvider);

		public TypeCode GetTypeCode() => TypeCode.Int32;
		public bool ToBoolean(IFormatProvider provider) => ((IConvertible)value).ToBoolean(provider);
		public byte ToByte(IFormatProvider provider) => ((IConvertible)value).ToByte(provider);
		public char ToChar(IFormatProvider provider) => ((IConvertible)value).ToChar(provider);
		public DateTime ToDateTime(IFormatProvider provider) => ((IConvertible)value).ToDateTime(provider);
		public decimal ToDecimal(IFormatProvider provider) => ((IConvertible)value).ToDecimal(provider);
		public double ToDouble(IFormatProvider provider) => ((IConvertible)value).ToDouble(provider);
		public short ToInt16(IFormatProvider provider) => ((IConvertible)value).ToInt16(provider);
		public int ToInt32(IFormatProvider provider) => value;
		public long ToInt64(IFormatProvider provider) => ((IConvertible)value).ToInt64(provider);
		public sbyte ToSByte(IFormatProvider provider) => ((IConvertible)value).ToSByte(provider);
		public float ToSingle(IFormatProvider provider) => ((IConvertible)value).ToSingle(provider);
		public string ToString(IFormatProvider provider) => value.ToString(provider);
		public object ToType(Type conversionType, IFormatProvider provider) => ((IConvertible)value).ToType(conversionType, provider);
		public ushort ToUInt16(IFormatProvider provider) => ((IConvertible)value).ToUInt16(provider);
		public uint ToUInt32(IFormatProvider provider) => ((IConvertible)value).ToUInt32(provider);
		public ulong ToUInt64(IFormatProvider provider) => ((IConvertible)value).ToUInt64(provider);
	}
}