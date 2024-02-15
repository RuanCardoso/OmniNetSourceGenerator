using Omni.Core;
using Programa.Zeth;

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

		// Sync Net Var with Roslyn Generators (:
		protected virtual bool OnPropertyChanged(string netVarName, int id) => true;
#pragma warning disable CA1822
#pragma warning disable IDE1006
		protected void ___2205032024<T>(T oldValue, T newValue, string type, string netVarName, int id, bool isPrimitiveType, bool isValueType, string typeKind) // called when 
#pragma warning restore IDE1006
#pragma warning restore CA1822
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

namespace a
{
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
}