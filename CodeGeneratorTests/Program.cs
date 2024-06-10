namespace NamespaceTests
{
    partial class Program : NetworkBehaviour
    {
        private int m_Health = 100;

        [NetVar(id: 10)]
        int Health { get; set; } = 100;

        private Dictionary<string, int> m_Mana = new();

        [NetVar(12)]
        Dictionary<string, int> Mana { get; set; } = new();

        static void Main()
        {
            Console.WriteLine("Hello World!");
        }
    }
}

public class NetVar : Attribute
{
    public NetVar(byte id) { }
}

public class NetworkBehaviour : NetVarBehaviour
{
    protected virtual void ___OnPropertyChanged___(NetworkBuffer buffer, NetworkPeer peer) { }
}

namespace MemoryPack { }

namespace Newtonsoft.Json { }

public class NetworkPeer { }

public class NetworkBuffer
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
