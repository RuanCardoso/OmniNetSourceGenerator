#nullable disable

using System.Collections;
using Omni.Net;

namespace Omni.Net
{
    public class Aaaa : ISerializable { }
}

public class NetworkManager
{
    public ISerializable SharedPeer;
}

public interface ISerializable { }

namespace NamespaceTests
{
    public partial class Program : ServerBehaviour
    {
        [NetworkVariable]
        private Aaaa m_Hel;

        private int m_Mana;

        [NetworkVariable]
        public int Mana
        {
            get { return m_Mana; }
            set { m_Mana = value; }
        }

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
    ) { }

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
    ) { }

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
    ) { }

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
    ) { }

    protected virtual void ___NotifyChange___() { }

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

public class DataBuffer
{
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
