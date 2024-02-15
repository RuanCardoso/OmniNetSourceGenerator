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
	public class SyncVar : Attribute
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