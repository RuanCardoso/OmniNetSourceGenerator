#pragma warning disable

using Omni.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
	public struct SyncOptions()
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

	/// <summary>
	/// Compares two values of type T for deep equality.
	/// </summary>
	/// <typeparam name="T">The type of the values to compare.</typeparam>
	/// <param name="oldValue">The old value to compare.</param>
	/// <param name="newValue">The new value to compare.</param>
	/// <returns>True if the values are deeply equal; otherwise, false.</returns>
	protected bool DeepEquals<T>(T oldValue, T newValue, string name)
	{
		return true;
	}

	protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name)
	{
		return false;
	}

	protected bool ___NotifyEditorChange___Called { get; set; } = false;
	protected virtual void ___NotifyEditorChange___()
	{
		___NotifyEditorChange___Called = false;
	}

	protected virtual void ___NotifyChange___() { }

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event2 Remote;
	public Event2 Local;
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

	protected virtual void ___NotifyChange___() { }
	protected bool ___NotifyEditorChange___Called { get; set; } = false;
	protected virtual void ___NotifyEditorChange___()
	{
		___NotifyEditorChange___Called = false;
	}


	/// <summary>
	/// Compares two values of type T for deep equality.
	/// </summary>
	/// <typeparam name="T">The type of the values to compare.</typeparam>
	/// <param name="oldValue">The old value to compare.</param>
	/// <param name="newValue">The new value to compare.</param>
	/// <returns>True if the values are deeply equal; otherwise, false.</returns>
	protected bool DeepEquals<T>(T oldValue, T newValue, string name)
	{
		return true;
	}

	protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name)
	{
		return false;
	}

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event2 Remote;
	public Event2 Local;
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

	/// <summary>
	/// Compares two values of type T for deep equality.
	/// </summary>
	/// <typeparam name="T">The type of the values to compare.</typeparam>
	/// <param name="oldValue">The old value to compare.</param>
	/// <param name="newValue">The new value to compare.</param>
	/// <returns>True if the values are deeply equal; otherwise, false.</returns>
	protected bool DeepEquals<T>(T oldValue, T newValue, string name)
	{
		return true;
	}

	protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name)
	{
		return false;
	}

	protected virtual void ___NotifyChange___() { }

	protected bool ___NotifyEditorChange___Called { get; set; } = false;
	protected virtual void ___NotifyEditorChange___()
	{
		___NotifyEditorChange___Called = false;
	}

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event2 Remote;
	public Event2 Local;
}

public class NetworkBehaviour : NetVarBehaviour
{
	public bool IsMine => false;
	public bool IsServer => false;

	public NetworkIdentity Identity { get; set; }

	protected virtual void ___OnPropertyChanged___(
		string propertyName,
		byte propertyId,
		NetworkPeer peer,
		DataBuffer buffer
	)
	{ }

	protected bool ___NotifyEditorChange___Called { get; set; } = false;
	protected virtual void ___NotifyEditorChange___()
	{
		___NotifyEditorChange___Called = false;
	}
	protected virtual void ___NotifyChange___() { }

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("Don't override this method! The source generator will override it.")]
	protected virtual void ___InjectServices___()
	{

	}

	/// <summary>
	/// Compares two values of type T for deep equality.
	/// </summary>
	/// <typeparam name="T">The type of the values to compare.</typeparam>
	/// <param name="oldValue">The old value to compare.</param>
	/// <param name="newValue">The new value to compare.</param>
	/// <returns>True if the values are deeply equal; otherwise, false.</returns>
	protected bool DeepEquals<T>(T oldValue, T newValue, string name)
	{
		return true;
	}

	protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name)
	{
		return false;
	}

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }

		public void Invoke(byte msgId, SyncOptions options) { }
	}

	public Event Remote;
	public Event Local;
}

public class Event2
{
	public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	public void Invoke(byte msgId, NetworkPeer peer, SyncOptions options) { }
	public void Invoke(byte msgId, SyncOptions options) { }
}