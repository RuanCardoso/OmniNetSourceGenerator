#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using Omni.Net;

namespace Omni.Net
{
	public class Aaaa : IMessage { }
}

// public partial class PlayerBase : NetworkBehaviour
// {
//     private int m_Healt;

//     [NetworkVariable]
//     public int Healt
//     {
//         get { return m_Healt; }
//         set { m_Healt = value; }
//     }
// }

public partial class Player : NetworkBehaviour
{
	[NetworkVariable]
	private int m_Health;

	[NetworkVariable]
	private int m_Health2;

	[NetworkVariable]
	private int m_Health3;

	[NetworkVariable]
	private Dictionary<string, int> m_Hea;

	void a()
	{
		Health = 1;
	}
}

public class NetworkManager
{
	public static NetworkPeer SharedPeer;
	public static NetworkPeer LocalPeer;
	public static NPool Pool;
}

public class NPool()
{
	public DataBuffer Rent()
	{
		return default;
	}
}

public interface IMessage { }

public interface IMessageWithPeer { }

namespace NamespaceTests
{
	public class PlayerBase : NetworkBehaviour { }

	public partial class Program : PlayerBase
	{
		// [NetworkVariable]
		// private Aaaa m_Hel;

		// private int m_Mana;

		// [NetworkVariable]
		// public int Mana
		// {
		//     get { return m_Mana; }
		//     set { m_Mana = value; }
		// }

		static void Main(string[] args) { }
	}
}

public class NetworkVariable : Attribute
{
	public NetworkVariable(bool track = false) { }

	public NetworkVariable(byte id) { }
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

	protected virtual void ___NotifyChange___() { }

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event Remote;
	public Event Local;
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

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event Remote;
	public Event Local;
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

	protected virtual void ___NotifyChange___() { }

	public class Event
	{
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
	}

	public Event Remote;
	public Event Local;
}

public class NetworkBehaviour : NetVarBehaviour
{
	public bool IsMine => false;
	public bool IsServer => false;

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

	public Event Remote;
	public Event Local;
}

public class NetworkVariableOptions
{
	public static NetworkVariableOptions Default { get; set; } = new NetworkVariableOptions();
}

namespace MemoryPack { }

namespace Newtonsoft.Json { }

public class NetworkPeer { }

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

public class Server : Attribute
{
	public Server(byte id) { }
}

public class Client : Attribute
{
	public Client(byte id) { }
}

public class NetVarBehaviour { }

interface ABBS { }
