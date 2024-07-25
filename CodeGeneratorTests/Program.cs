#nullable disable

using System.Collections;

namespace NamespaceTests
{
    public partial class Program : ServerBehaviour
    {
        private int m_Hel;

        [NetworkVariable]
        public int Hel
        {
            get { return m_Hel; }
            set { m_Hel = value; }
        }

        [NetworkVariable]
        public int MyLifeN
        {
            get { return m_MyLifeN; }
            set { m_MyLifeN = value; }
        }

        [NetworkVariable]
        public Dictionary<int, string> MyLifeTests
        {
            get => m_MyLifeTests;
            set => m_MyLifeTests = value;
        }

        private Dictionary<int, string> m_MyLifeTests;

        [NetworkVariable(76)]
        private int m_Mana2;

        [NetworkVariable(128)]
        private int m_Mana1,
            m_Mana3,
            m_Mana4,
            m_Mana5;

        private int m_MyLifeN;

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
    protected virtual void ___NotifyChange___() { }

    protected virtual void ___OnPropertyChanged___(
        string propertyName,
        byte propertyId,
        NetworkPeer peer,
        DataBuffer buffer
    ) { }

    public class Event
    {
        public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options) { }
    }

    public Event Remote;
    public Event Local;
}

public class DualBehaviour
{
    protected virtual void ___NotifyChange___() { }

    protected virtual void ___OnPropertyChanged___(
        string propertyName,
        byte propertyId,
        NetworkPeer peer,
        DataBuffer buffer
    ) { }

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

    protected virtual void ___OnPropertyChanged___(DataBuffer buffer, NetworkPeer peer) { }

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
