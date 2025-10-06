using System;

namespace Omni.Inspector
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class Group : Attribute
	{
		public string name;

		public Group(string name) => this.name = name;
	}
}

namespace UnityEngine.Scripting
{
	public class PreserveAttribute : Attribute
	{
		public bool AllMembers;
		public bool Conditional;
	}
}

namespace UnityEngine.Scripting
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public class AlwaysLinkAssembly : Attribute { }
}

namespace UnityEngine
{
	public enum RuntimeInitializeLoadType
	{
		BeforeSceneLoad,
		AfterSceneLoad
	}

	[System.AttributeUsage(System.AttributeTargets.Method)]
	public class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
	{
		public RuntimeInitializeLoadType loadType { get; }

		public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType = RuntimeInitializeLoadType.BeforeSceneLoad)
		{
			this.loadType = loadType;
		}
	}
}

namespace Omni.Core
{
	// Marcada no lado do servidor para indicar qual classe equivalente do lado do cliente será usada
	public sealed class GenRpcAttribute : Attribute
	{
		public GenRpcAttribute(string classname)
		{

		}
	}

	[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public sealed class DeltaSerializable : Attribute
	{
		/// <summary>
		/// Gets or sets a value indicating whether the struct should be serialized.
		/// </summary>
		/// <value>
		/// Default is <c>true</c>, meaning the struct will be serialized using delta compression.
		/// If <c>false</c>, the struct will be serialized in full.
		/// </value>
		public bool Enabled { get; set; } = true;
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public sealed class Model : Attribute
	{

	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class SerializeProperty : Attribute
	{

	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class HidePicker : Attribute
	{

	}



	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ServiceAttribute : Attribute
	{
		internal string ServiceName { get; }

		public ServiceAttribute()
		{
			ServiceName = string.Empty;
		}

		public ServiceAttribute(string serviceName)
		{
			ServiceName = serviceName;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class GlobalServiceAttribute : ServiceAttribute
	{
		public GlobalServiceAttribute() { }
		public GlobalServiceAttribute(string serviceName) : base(serviceName)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class LocalServiceAttribute : ServiceAttribute
	{
		public LocalServiceAttribute() { }
		public LocalServiceAttribute(string serviceName) : base(serviceName)
		{
		}
	}

	public enum DeliveryMode : byte
	{
		/// <summary>
		/// Ensures packets are delivered reliably and in the exact order they were sent.
		/// No packets will be dropped, duplicated, or arrive out of order.
		/// </summary>
		ReliableOrdered,

		/// <summary>
		/// Sends packets without guarantees. Packets may be dropped, duplicated, or arrive out of order.
		/// This mode offers the lowest latency but no reliability.
		/// </summary>
		Unreliable,

		/// <summary>
		/// Ensures packets are delivered reliably but without enforcing any specific order.
		/// Packets won't be dropped or duplicated, but they may arrive out of sequence.
		/// </summary>
		ReliableUnordered,

		/// <summary>
		/// Sends packets without reliability but guarantees they will arrive in order.
		/// Packets may be dropped, but no duplicates will occur, and order is preserved.
		/// </summary>
		Sequenced,

		/// <summary>
		/// Ensures only the latest packet in a sequence is delivered reliably and in order.
		/// Intermediate packets may be dropped, but duplicates will not occur, and the last packet is guaranteed.
		/// This mode does not support fragmentation.
		/// </summary>
		ReliableSequenced
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class EventAttribute : Attribute
	{
		internal byte Id { get; }

		/// <summary>
		/// Gets or sets the delivery mode for this RPC.
		/// </summary>
		/// <value>
		/// Default is <c>ReliableOrdered</c>, ensuring the RPC is delivered reliably and in order.
		/// </value>
		public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.ReliableOrdered;

		/// <summary>
		/// Gets or sets the sequence channel used for this RPC.
		/// </summary>
		/// <value>
		/// Default is <c>0</c>. Different channels can be used to separate ordering concerns.
		/// </value>
		public byte SequenceChannel { get; set; } = 0;

		public EventAttribute() { }
		public EventAttribute(byte id)
		{

		}
	}

	/// <summary>
	/// Marks a method as a client-side Remote Procedure Call (RPC) event.
	/// </summary>
	/// <remarks>
	/// Use this attribute to indicate that a method is intended to handle RPCs sent to the client.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class ClientAttribute : EventAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ClientAttribute"/> class. with an automatically generated identifier.
		/// </summary>
		public ClientAttribute() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientAttribute"/> class with the specified rpc ID.
		/// </summary>
		/// <param name="id">The unique identifier for the client-side RPC event.</param>
		public ClientAttribute(byte id) : base(id)
		{
		}
	}

	/// <summary>
	/// Marks a method as a server-side Remote Procedure Call (RPC) event.
	/// </summary>
	/// <remarks>
	/// Use this attribute to indicate that a method is intended to handle RPCs sent to the server.
	/// By default, ownership verification is required for the RPC to be accepted.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class ServerAttribute : EventAttribute
	{
		/// <summary>
		/// Gets or sets a value indicating whether ownership is required to invoke the server RPC.
		/// </summary>
		/// <value>
		/// Default is <c>true</c>, meaning only the client with ownership can call this RPC.
		/// </value>
		public bool RequiresOwnership { get; set; } = true;

		/// <summary>
		/// Gets or sets the target for this server RPC.
		/// </summary>
		/// <value>
		/// Default is <c>Target.Auto</c>, which automatically determines the appropriate target based on context.
		/// </value>
		public Target Target { get; set; } = Target.Auto;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerAttribute"/> class. with an automatically generated identifier.
		/// </summary>
		public ServerAttribute() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerAttribute"/> class with the specified rpc ID.
		/// </summary>
		/// <param name="id">The unique identifier for the server-side RPC event.</param>
		public ServerAttribute(byte id) : base(id)
		{
		}
	}

	[Flags]
	public enum HideMode
	{
		/// <summary>
		/// No parts are hidden; both the backing field and property are visible in the Inspector.
		/// </summary>
		None = 0,
		/// <summary>
		/// Hides the backing field from the Unity Inspector.
		/// </summary>
		BackingField = 1,
		/// <summary>
		/// Hides the property from the Unity Inspector.
		/// </summary>
		Property = 2,
		/// <summary>
		/// Hides both the backing field and the property from the Unity Inspector.
		/// </summary>
		Both = 4
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
	public class NetworkVariableAttribute : Attribute
	{
		public byte Id { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the property requires client authority 
		/// for synchronization.
		/// </summary>
		/// <value>
		/// Default is <c>false</c>, meaning client authority is not required.
		/// </value>
		public bool IsClientAuthority { get; set; } = false;

		public bool ServerBroadcastsClientUpdates { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether ownership is required for property synchronization.
		/// </summary>
		/// <value>
		/// Default is <c>true</c>, meaning only the owner can modify the synchronized variable.
		/// </value>
		public bool RequiresOwnership { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether equality checks should be performed before synchronizing the value.
		/// </summary>
		/// <value>
		/// Default is <c>true</c>, meaning the value is synchronized only if it has changed.
		/// </value>
		public bool CheckEquality { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the field should be hidden in the Unity Inspector.
		/// </summary>
		/// <value>
		/// Default is <c>HideMode.BackingField</c>, meaning the field will not be visible in the Unity Inspector.
		/// </value>
		public HideMode HideMode { get; set; } = HideMode.BackingField;

		public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.ReliableOrdered;

		public Target Target { get; set; } = Target.Auto;

		public byte SequenceChannel { get; set; } = 0;

		public NetworkVariableAttribute()
		{
			Id = 0;
		}

		public NetworkVariableAttribute(byte id) { }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	internal class GenerateSecureKeysAttribute : Attribute
	{
	}
}