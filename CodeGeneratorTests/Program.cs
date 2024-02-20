using System.Text;
using Teste;

namespace MessagePack
{

}

namespace Newtonsoft.Json
{

}

namespace Teste
{
	[Remote(Self = false, Name = "Sync1", Id = 10)]
	[Remote(Self = false, Name = "Sync2", Id = 5)]
	public partial class Programa : NetworkBehaviour
	{
		[NetVar]
		protected int m_IntNetVar;

		[NetVar]
		protected float m_FloatNetVar;

		[NetVar]
		protected double m_DoubleNetVar;

		[NetVar]
		protected decimal m_DecimalNetVar;

		[NetVar]
		protected bool m_BoolNetVar;

		[NetVar]
		protected char m_CharNetVar;

		[NetVar]
		protected byte m_ByteNetVar;

		[NetVar]
		protected sbyte m_SByteNetVar;

		[NetVar]
		protected short m_ShortNetVar;

		[NetVar]
		protected ushort m_UShortNetVar;

		[NetVar]
		protected uint m_UIntNetVar;

		[NetVar]
		protected long m_LongNetVar;		
		
		[NetVar(SerializeAsJson = false)]
		private Action<int, int, Tipo1> OnEnteredPlayer;

		[NetVar(SerializeAsJson = true)]
		private Action<int, int, Tipo1> OnEnteredPlayer2;

		[NetVar]
		protected ulong m_ULongNetVar;

		[NetVar]
		private Tipo1 tipo1;

		[NetVar(SerializeAsJson = false, CustomSerializeAndDeserialize = true)]
		private Tipo2 tipo2;

		[NetVar(SerializeAsJson = true, CustomSerializeAndDeserialize = true)]
		public Dictionary<int, object> m_Equips;



		partial void Sync1_Client(IDataReader reader, NetworkPeer peer)
		{
			throw new NotImplementedException();
		}

		static void Main()
		{
			try
			{
				Console.WriteLine("Hello!");
			}
			catch (Exception) { }
		}

		void dd()
		{

		}

	}

	internal struct Tipo2
	{
	}

	internal class Tipo1
	{
	}

	internal class DataWriterPool
	{
		internal static IDataWriter Get()
		{
			throw new NotImplementedException();
		}
	}

	public class NetworkBehaviour
	{
		// Sync NetVar with Roslyn Generators (:
		// Roslyn methods!
		protected virtual bool OnPropertyChanged(string netVarName, int id) => true;
#pragma warning disable CA1822
#pragma warning disable IDE1006
		internal void Internal___2205032023(byte id, IDataReader dataReader) => ___2205032023(id, dataReader);
		protected virtual void ___2205032023(byte id, IDataReader dataReader) { throw new NotImplementedException("NetVar not implemented!"); } // Deserialize

		public bool IsServer;

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, byte rpcId, byte channel = 0)
		{

		}

		public void Rpc(IDataWriter writer, DataDeliveryMode dataDeliveryMode, int playerId, byte rpcId, byte channel = 0)
		{

		}

		protected virtual void OnCustomSerialize(byte id, IDataWriter writer, int argIndex = 0) // argIndex to delegates
		{

		}

		protected virtual void OnCustomDeserialize(byte id, IDataReader reader, int argIndex = 0) // argIndex to delegates
		{

		}

		protected void ___2205032024(IDataWriter writer, byte id) { }

		internal IDataWriter GetWriter()
		{
			throw new NotImplementedException();
		}

		internal IDataReader GetReader()
		{
			throw new NotImplementedException();
		}

		internal void Release(IDataWriter writer)
		{
			throw new NotImplementedException();
		}

		internal void Release(IDataReader reader)
		{
			throw new NotImplementedException();
		}
	}

	public class MessagePackSerializer
	{
		public static MessagePackSerializerOptions DefaultOptions;
	}

	public class Network : NetworkBehaviour
	{

	}
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RemoteAttribute : Attribute
{
	public byte Id { get; set; }
	public string Name { get; set; }
	public bool Self { get; set; }
}

// Roslyn Generated //  Roslyn Analyzer
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class NetVarAttribute : Attribute
{
	public bool SerializeAsJson { get; set; }
	public bool CustomSerializeAndDeserialize { get; set; }
}

public interface IDataWriter
{
	byte[] Buffer { get; }
	int Position { get; }
	int BytesWritten { get; }
	Encoding Encoding { get; }
	void Clear();
	void Write<T>(T notSupported); // Roslyn NetVarTypes
	void Write(byte[] buffer, int offset, int count);
	void Write(Span<byte> value);
	void Write(char value);
	void Write(byte value);
	void Write(short value);
	void Write(int value);
	void Write(long value);
	void Write(double value);
	void Write(float value);
	void Write(decimal value);
	void Write(sbyte value);
	void Write(ushort value);
	void Write(uint value);
	void Write(ulong value);
	void Write(string value);
	void WriteWithoutAllocation(string value);
	void SerializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class;
	void SerializeWithJsonNet<T>(T data, JsonSerializerSettings options);
	void SerializeWithMsgPack<T>(T data, MessagePackSerializerOptions options);
	void Write(bool value);
	void Write(bool v1, bool v2);
	void Write(bool v1, bool v2, bool v3);
	void Write(bool v1, bool v2, bool v3, bool v4);
	void Write(bool v1, bool v2, bool v3, bool v4, bool v5);
	void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6);
	void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7);
	void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7, bool v8);
	void Write7BitEncodedInt(int value);
	void Write7BitEncodedInt64(long value);
	void Marshalling_Write<T>(T structure) where T : struct;

}
public interface IDataReader
{
	byte[] Buffer { get; }
	int Position { get; }
	int BytesWritten { get; }
	Encoding Encoding { get; }
	void Recycle();
	void Write(byte[] buffer, int offset, int count);
	void Read(byte[] buffer, int offset, int count);
	int Read(Span<byte> value);
	T ReadCustomMessage<T>() where T : unmanaged, IComparable, IConvertible, IFormattable;
	char ReadChar();
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
	T DeserializeWithJsonNet<T>(JsonSerializerSettings options);
	T DeserializeWithMsgPack<T>(MessagePackSerializerOptions options);
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

public interface NetworkPeer { }
public class DataDeliveryMode
{
}

[Remote(Self = false, Name = "Sync3", Id = 10)]
[Remote(Self = false, Name = "Sync4", Id = 5)]
public partial class RpcTestes : NetworkBehaviour
{
	[NetVar(SerializeAsJson = true)]
	private float m_health, m_Ammo;
	void dd()
	{

	}
}