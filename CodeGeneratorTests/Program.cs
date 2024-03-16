using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Security.Principal;
using System.Text;
using Teste;

namespace MessagePack
{

}

namespace MemoryPack
{

}


namespace Newtonsoft.Json
{

}



public enum DataTarget : byte
{
	/// <summary>
	/// Only the server will receive the data.
	/// </summary>
	Server = 0,
	/// <summary>
	/// All clients will receive the data.
	/// </summary>
	Broadcast = 1,
	/// <summary>
	/// All clients will receive the data, except the sender.
	/// </summary>
	BroadcastExcludingSelf = 2,
	/// <summary>
	/// Only the sender will receive the data.
	/// </summary>
	Self = 3,
}

namespace Teste
{
	[Remote("Move", 8, self: false)]
	[Remote("Color", self: true, id: 19)]
	[Remote("Rotation", id: 32)]
	public partial class Programa : NetworkBehaviour
	{
		[NetVar(SerializerType.Json, TargetMode.Server, sequenceChannel: 9)]
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

		[NetVar]
		protected ulong m_ULongNetVar;

		[NetVar(Serializer = SerializerType.Json, Target = TargetMode.BroadcastExcludingSelf, Delivery = DeliveryMode.ReliableEncryptedUnordered)]
		private Tipo1 tipo1;

		[NetVar]
		private Tipo2 tipo2;

		[NetVar]
		public Dictionary<int, object> m_Equips;

		[NetVar]
		private Action<Tipo2> OnEnteredPlayer;

		[NetVar]
		private Action OnEnteredPlayer2;

		[NetVar]
		protected Vector3 m_UVectorNetVar;

		[NetVar]
		protected Vector2 m_UVector2NetVar;

		[NetVar]
		protected Quaternion m_UQNetVar;

		[NetVar(Serializer = SerializerType.Json, Target = TargetMode.BroadcastExcludingSelf, Delivery = DeliveryMode.ReliableEncryptedUnordered)]
		protected Color m_UCNetVar;

		//partial void Sync1_Client(IDataReader reader, NetworkPeer peer)
		//{
		//	throw new NotImplementedException();
		//}

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

	public class DataWriter : IDataWriter
	{
		public static DataWriter Empty;

		public byte[] Buffer => throw new NotImplementedException();

		public int Position => throw new NotImplementedException();

		public int BytesWritten => throw new NotImplementedException();

		public Encoding Encoding => throw new NotImplementedException();

		public bool IsReleased { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		int IDataWriter.Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		int IDataWriter.BytesWritten { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		Encoding IDataWriter.Encoding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public void Marshalling_Write<T>(T structure) where T : struct
		{
			throw new NotImplementedException();
		}

		public void SerializeWithCustom<T>(ISyncCustom ISyncCustom) where T : class
		{
			throw new NotImplementedException();
		}

		public void SerializeWithJsonNet<T>(T data, JsonSerializerSettings options)
		{
			throw new NotImplementedException();
		}

		public void SerializeWithMsgPack<T>(T data, MessagePackSerializerOptions options)
		{
			throw new NotImplementedException();
		}

		public void ToBinary<T>(T data, MemoryPackSerializerOptions options = null)
		{
			throw new NotImplementedException();
		}

		public void ToBinary2<T>(T data, MessagePackSerializerOptions options = null)
		{
			throw new NotImplementedException();
		}

		public string ToJson<T>(T data, JsonSerializerSettings options = null)
		{
			throw new NotImplementedException();
		}

		public void Write<T>(T notSupported)
		{
			throw new NotImplementedException();
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public void Write(Span<byte> value)
		{
			throw new NotImplementedException();
		}

		public void Write(char value)
		{
			throw new NotImplementedException();
		}

		public void Write(byte value)
		{
			throw new NotImplementedException();
		}

		public void Write(short value)
		{
			throw new NotImplementedException();
		}

		public void Write(int value)
		{
			throw new NotImplementedException();
		}

		public void Write(long value)
		{
			throw new NotImplementedException();
		}

		public void Write(double value)
		{
			throw new NotImplementedException();
		}

		public void Write(float value)
		{
			throw new NotImplementedException();
		}

		public void Write(decimal value)
		{
			throw new NotImplementedException();
		}

		public void Write(sbyte value)
		{
			throw new NotImplementedException();
		}

		public void Write(ushort value)
		{
			throw new NotImplementedException();
		}

		public void Write(uint value)
		{
			throw new NotImplementedException();
		}

		public void Write(ulong value)
		{
			throw new NotImplementedException();
		}

		public void Write(string value)
		{
			throw new NotImplementedException();
		}

		public void Write(bool value)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2, bool v3)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2, bool v3, bool v4)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2, bool v3, bool v4, bool v5)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7)
		{
			throw new NotImplementedException();
		}

		public void Write(bool v1, bool v2, bool v3, bool v4, bool v5, bool v6, bool v7, bool v8)
		{
			throw new NotImplementedException();
		}

		public void Write(ReadOnlySpan<byte> value)
		{
			throw new NotImplementedException();
		}

		public void Write(Stream value)
		{
			throw new NotImplementedException();
		}

		public void Write(Vector3 value)
		{
			throw new NotImplementedException();
		}

		public void Write(Vector2 value)
		{
			throw new NotImplementedException();
		}

		public void Write(Quaternion value)
		{
			throw new NotImplementedException();
		}

		public void Write(Color color)
		{
			throw new NotImplementedException();
		}

		public void Write<T>(ISyncCustom ISyncCustom) where T : class
		{
			throw new NotImplementedException();
		}

		public void Write7BitEncodedInt(int value)
		{
			throw new NotImplementedException();
		}

		public void Write7BitEncodedInt64(long value)
		{
			throw new NotImplementedException();
		}

		public string WriteNonAlloc(string value)
		{
			throw new NotImplementedException();
		}

		public void WriteWithoutAllocation(string value)
		{
			throw new NotImplementedException();
		}

		string IDataWriter.Write(string value)
		{
			throw new NotImplementedException();
		}
	}

	public struct Tipo2
	{
	}

	public class Tipo1
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

		/// <summary>
		/// Sends a client RPC (Remote Procedure Call) to the server.
		/// </summary>
		/// <param name="writer">The data writer for the RPC message.</param>
		/// <param name="deliveryMode">The delivery mode for the RPC message.</param>
		/// <param name="targetMode">The target mode for the RPC message.</param>
		/// <param name="rpcId">The ID of the RPC message.</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC message (default is 0).</param>
		public void ClientRpc(IDataWriter writer, DeliveryMode deliveryMode, TargetMode targetMode, byte rpcId,
			byte sequenceChannel = 0)
		{

		}

		/// <summary>
		/// Send a remote procedure call (RPC) from the client to the server.
		/// </summary>
		/// <param name="writer">The data writer to use for sending the RPC.</param>
		/// <param name="rpcId">The ID of the RPC to send.</param>
		/// <param name="deliveryMode">The delivery mode for the RPC (default is ReliableOrdered).</param>
		/// <param name="targetMode">The target mode for the RPC (default is Broadcast).</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC (default is 0).</param>
		public void ClientRpc(IDataWriter writer, byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			TargetMode targetMode = TargetMode.Broadcast, byte sequenceChannel = 0)
		{
		}

		/// <summary>
		/// Sends a client RPC with the specified RPC ID to the server, delivery mode, target mode, and sequence channel.
		/// </summary>
		/// <param name="rpcId">The ID of the RPC to be sent</param>
		/// <param name="deliveryMode">The delivery mode for the RPC (default: ReliableOrdered)</param>
		/// <param name="targetMode">The target mode for the RPC (default: Broadcast)</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC (default: 0)</param>
		public void ClientRpc(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			TargetMode targetMode = TargetMode.Broadcast, byte sequenceChannel = 0)
		{
		}

		/// <summary>
		/// Sends a server RPC to a specific peer.
		/// </summary>
		/// <param name="writer">The data writer to use for sending the RPC.</param>
		/// <param name="deliveryMode">The delivery mode of the RPC.</param>
		/// <param name="peerId">The ID of the peer to send the RPC to.</param>
		/// <param name="rpcId">The ID of the RPC to send.</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC (optional, default is 0).</param>
		public void ServerRpc(IDataWriter writer, DeliveryMode deliveryMode, int peerId, byte rpcId,
			byte sequenceChannel = 0)
		{

		}

		/// <summary>
		/// Sends a server RPC to the specified target using the provided data writer, delivery mode, target mode, RPC ID, and sequence channel.
		/// Server RPCs is sent from the server to the client's.
		/// </summary>
		/// <param name="writer">The data writer used to write the RPC data.</param>
		/// <param name="deliveryMode">The delivery mode for the RPC.</param>
		/// <param name="targetMode">The target mode for the RPC.</param>
		/// <param name="rpcId">The unique ID of the RPC.</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC (optional, default is 0).</param>
		public void ServerRpc(IDataWriter writer, DeliveryMode deliveryMode, TargetMode targetMode, byte rpcId,
			byte sequenceChannel = 0)
		{

		}

		/// <summary>
		/// Sends a server RPC to the client with the specified RPC ID, delivery mode, target mode, and sequence channel.
		/// </summary>
		/// <param name="writer">The data writer for the RPC.</param>
		/// <param name="rpcId">The ID of the RPC.</param>
		/// <param name="deliveryMode">The delivery mode for the RPC (default is ReliableOrdered).</param>
		/// <param name="targetMode">The target mode for the RPC (default is Broadcast).</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC (default is 0).</param>
		public void ServerRpc(IDataWriter writer, byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			TargetMode targetMode = TargetMode.Broadcast, byte sequenceChannel = 0)
		{
		}

		/// <summary>
		/// Sends a server RPC to the client with the specified RPC ID, delivery mode, target mode, and sequence channel.
		/// </summary>
		/// <param name="rpcId">The ID of the RPC to send.</param>
		/// <param name="deliveryMode">The delivery mode for the RPC (default is ReliableOrdered).</param>
		/// <param name="targetMode">The target mode for the RPC (default is Broadcast).</param>
		/// <param name="sequenceChannel">The sequence channel for the RPC (default is 0).</param>
		public void ServerRpc(byte rpcId, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			TargetMode targetMode = TargetMode.Broadcast, byte sequenceChannel = 0)
		{
		}

		protected virtual void OnCustomSerialize(byte id, IDataWriter writer, int argIndex = 0) // argIndex to delegates
		{

		}

		protected virtual void OnCustomDeserialize(byte id, IDataReader reader, int argIndex = 0) // argIndex to delegates
		{

		}

		protected void ___2205032024(IDataWriter writer, byte id, DeliveryMode deliveryMode, TargetMode targetMode, byte sequenceChannel = 0) { }

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

	public class MemoryPackSerializer
	{

	}

	public class Network : NetworkBehaviour
	{

	}
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RemoteAttribute : Attribute
{
	public RemoteAttribute(byte id)
	{
		Id = id;
	}


	public RemoteAttribute(string name, byte id = 0, bool self = false)
	{
		Name = name;
		Id = id;
		Self = self;
	}

	public string Name { get; set; }
	public byte Id { get; set; }
	public bool Self { get; set; }
}

// Roslyn Generated //  Roslyn Analyzer
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class NetVarAttribute : Attribute
{
	public NetVarAttribute(SerializerType serializer = SerializerType.MemPack, TargetMode target = TargetMode.Broadcast, DeliveryMode delivery = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
	{
		Serializer = serializer;
		Target = target;
		Delivery = delivery;
		SequenceChannel = sequenceChannel;
	}

	public SerializerType Serializer { get; set; } = SerializerType.MemPack;
	public TargetMode Target { get; set; } = TargetMode.Broadcast;
	public DeliveryMode Delivery { get; set; } = DeliveryMode.ReliableOrdered;
	public byte SequenceChannel { get; set; } = 0;
}
public enum SerializerType : byte
{
	Json,
	MsgPack,
	MemPack,
}


public interface IDataWriter
{
	byte[] Buffer { get; }
	int Position { get; set; }
	int BytesWritten { get; set; }
	bool IsReleased { get; set; }
	Encoding Encoding { get; set; }
	void Clear();
	void Write(byte[] buffer, int offset, int count);
	void Write(Span<byte> value);
	void Write(ReadOnlySpan<byte> value);
	void Write(Stream value);
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
	string Write(string value);
	void Write(Vector3 value);
	void Write(Vector2 value);
	void Write(Quaternion value);
	void Write(Color color);
	string WriteNonAlloc(string value);
	void Write<T>(ISyncCustom ISyncCustom) where T : class;
	string ToJson<T>(T data, JsonSerializerSettings options = null);
	void ToBinary2<T>(T data, MessagePackSerializerOptions options = null);
	void ToBinary<T>(T data, MemoryPackSerializerOptions options = null);
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
	int Position { get; set; }
	int BytesWritten { get; set; }
	bool ResetPositionAfterWriting { get; set; }
	bool IsReleased { get; set; }
	Encoding Encoding { get; set; }
	void Clear();
	void Write(byte[] buffer, int offset, int count);
	void Write(Span<byte> value);
	void Write(ReadOnlySpan<byte> value);
	void Write(Stream value);
	void Read(byte[] buffer, int offset, int count);
	int Read(Span<byte> value);
	T ReadId<T>(out int lastPos) where T : unmanaged, IComparable, IConvertible, IFormattable;
	T ReadId<T>() where T : unmanaged, IComparable, IConvertible, IFormattable;
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
	Vector3 ReadVector3();
	Vector2 ReadVector2();
	Quaternion ReadQuaternion();
	Color ReadColor();
	string ReadStringNonAlloc();
	void ReadCustom<T>(ISyncCustom ISyncCustom) where T : class;
	T ReadJson<T>(JsonSerializerSettings options = null);
	T ReadBinary2<T>(MessagePackSerializerOptions options = null);
	T ReadBinary<T>(MemoryPackSerializerOptions options = null);
	uint ReadUInt();
	ulong ReadULong();
	bool ReadBool();
	void ReadBool(out bool v1, out bool v2);
	void ReadBool(out bool v1, out bool v2, out bool v3);
	void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4);
	void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5);
	void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6);
	void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7);

	void ReadBool(out bool v1, out bool v2, out bool v3, out bool v4, out bool v5, out bool v6, out bool v7,
		out bool v8);

	int Read7BitEncodedInt();
	long Read7BitEncodedInt64();
	T Marshalling_ReadStructure<T>() where T : struct;

}

public interface NetworkPeer { }
public class DataDeliveryMode
{
}

public partial class RpcTestes : NetworkBehaviour
{
	[NetVar]
	private float m_health, m_Ammo;

	[NetVar]
	private Action OnCreated;
	void dd()
	{

	}
}