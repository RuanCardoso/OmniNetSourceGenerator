using Omni.Core;
using Programa.Zeth;
using System.Text;

namespace Azeth
{
	public class Player : IEquatable<Player>
	{
		public bool Equals(Player? other)
		{
			throw new NotImplementedException();
		}
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
		public bool SerializeAsJson { get; set; }
		public bool VerifyIfEqual { get; set; }
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
		protected virtual void ___2205032023(byte id, IDataReader dataReader) { throw new NotImplementedException("NetVar not implemented!"); } // Deserialize
		protected void ___2205032024<T>(T oldValue, T newValue, string type, string netVarName, int id, bool isPrimitiveType, bool isValueType, string typeKind, bool serializeAsJson) // called when 
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

	public interface IDataReader
	{
		byte[] Buffer { get; }
		int Position { get; set; }
		int BytesWritten { get; set; }
		bool ResetPositionAfterWriting { get; set; }
		Encoding Encoding { get; set; }
		void Clear();
		void Write(byte[] buffer, int offset, int count);
		void Write(Span<byte> value);
		void Write(ReadOnlySpan<byte> value);
		void Write(Stream value);
		void Read(byte[] buffer, int offset, int count);
		int Read(Span<byte> value);
		T ReadCustomMessage<T>() where T : unmanaged, IComparable, IConvertible, IFormattable;
		byte ReadByte();
		short ReadShort();
		int ReadInt();
		long ReadLong();
		double ReadDouble();
		float ReadFloat();
		decimal ReadDecimal();
		sbyte ReadSByte();
		ushort ReadUShort();
		string ReadString();
		string ReadStringWithoutAllocation();
		void DeserializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class;
		T DeserializeWithJsonNet<T>(JsonSerializerSettings options = null);
		T DeserializeWithMsgPack<T>(MessagePackSerializerOptions options = null);
		uint ReadUInt();
		ulong ReadULong();
		bool ReadBool();
		void ReadBool(out bool v1, out bool v2);
		void ReadBool(out bool v1, out bool v2, out bool v3);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7);
		void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7, out bool v8);
		int Read7BitEncodedInt();
		long Read7BitEncodedInt64();
		T Marshalling_ReadStructure<T>() where T : struct;

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