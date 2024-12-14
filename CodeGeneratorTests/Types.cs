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
	}
}

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
	public event Action<int, int> OnItemAdded;
	public event Action<int, int> OnItemRemoved;
	public event Action<int, int> OnItemUpdated;
}

public class NetVarBehaviour { }
public class DataBuffer : IDisposable
{
	public int Length { get; set; }

	public T Read<T>()
	{
		return default;
	}

	public T ReadAsBinary<T>()
	{
		return default;
	}

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

public class NetworkManager
{
	public static NetworkPeer SharedPeer;
	public static NetworkPeer LocalPeer;
	public static NetPool Pool;
}

public class NetPool()
{
	public DataBuffer Rent()
	{
		return default;
	}
}

public class ServerBehaviour
{
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
	}

	public Event2 Server;
	public Event2 Client;
}

public class ClientBehaviour
{
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

	public Event2 Server;
	public Event2 Client;
}

public class DualBehaviour
{
	protected virtual void ___OnPropertyChanged___(
		string propertyName,
		byte propertyId,
		NetworkPeer peer,
		DataBuffer buffer
	)
	{ }

	protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name, byte id)
	{
		return false;
	}

	protected virtual void ___NotifyCollectionChange___() { }

	public class Event
	{
		public void NetworkVariableSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event2 Server;
	public Event2 Client;
}

public class NetworkBehaviour : NetVarBehaviour
{
	public bool IsMine => false;
	public bool IsServer => false;
	public bool IsClient => false;

	public NetworkIdentity Identity { get; set; }

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

	public class Event
	{
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

	public Event Server;
	public Event Client;
}

public class Event2
{
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
