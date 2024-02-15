using a.a.e.d.aaaa;
using Omni.Core;

namespace Azeth
{
	public class Player
	{

	}
}

namespace Omni.Core
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class Remote : Attribute
	{
		public byte Id { get; set; }
		public string? Name { get; set; } // Roslyn Analyzer
		public bool Self { get; set; } // Roslyn Analyzer
	}



	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class NetVar : Attribute
	{

	}

	public class NetworkBehaviour
	{
		public bool IsServer;

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte rpcId, byte channel)
		{

		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int playerId, byte rpcId, byte channel)
		{

		}
	}

	public class DataDeliveryMode
	{
	}

	public class NetworkPeer
	{

	}

	public class IDataReader
	{

	}

	public class IDataWriter
	{

	}
}

//namespace a
//{
[Remote(Id = 1, Name = "Syncot2", Self = true)]
[Remote(Id = 3, Name = "Syncove", Self = false)]
public partial class TesteClass23 : NetworkBehaviour
{
	[NetVar]
	private int teste = 100;

	[NetVar]
	private string teste2 = "100";

	[NetVar]
	private aa teste3 = aa.a;

	[NetVar]
	private bb teste4 = new();

	[NetVar]
	private vv teste5 = new();

	partial void Syncot2(IDataReader reader, NetworkPeer peer)
	{
		throw new NotImplementedException();
	}

	void Test()
	{

	}
}
//}