#nullable disable

using System.Collections;

namespace NamespaceTests
{
    public partial class Program : NetworkBehaviour
    {
        [NetworkVariable(5)]
        private int m_Mana2;

        [NetworkVariable(34)]
        private int m_Mana1,
            m_Mana3,
            m_Mana4,
            m_Mana5;

        [NetworkVariable]
        public int m_LifeZ;

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
    protected virtual void ___OnPropertyChanged___(DataBuffer buffer, NetworkPeer peer) { }

    public class Event
    {
        public void ManualSync<T>(T property, byte propertyId, SyncOptions options) { }
    }

    public Event Remote;
    public Event Local;
}

public class ClientBehaviour
{
    protected virtual void ___OnPropertyChanged___(DataBuffer buffer, NetworkPeer peer) { }

    public class Event
    {
        public void ManualSync<T>(T property, byte propertyId, SyncOptions options) { }
    }

    public Event Remote;
    public Event Local;
}

public class DualBehaviour
{
    protected virtual void ___OnPropertyChanged___(DataBuffer buffer, NetworkPeer peer) { }

    public class Event
    {
        public void ManualSync<T>(T property, byte propertyId, SyncOptions options) { }
    }

    public Event Remote;
    public Event Local;
}

public class NetworkBehaviour : NetVarBehaviour
{
    public bool IsMine => false;

    protected virtual void ___OnPropertyChanged___(DataBuffer buffer, NetworkPeer peer) { }

    public class Event
    {
        public void ManualSync<T>(T property, byte propertyId, SyncOptions options) { }
    }

    public Event Remote;
    public Event Local;
}

public struct SyncOptions
{
    public static SyncOptions Default => new SyncOptions();
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

    public T FromBinary<T>()
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
