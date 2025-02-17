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


namespace Omni.Core
{
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

	public class Server : Attribute
	{
		internal byte Id { get; }
		public Server(byte id) { }
	}

	public class Client : Attribute
	{
		internal byte Id { get; }
		public Client(byte id) { }
	}

	public class NetworkVariable : Attribute
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
		/// Default is <c>true</c>, meaning the field will not be visible in the Unity Inspector.
		/// </value>
		public bool HideInInspector { get; set; } = true;

		public NetworkVariable(byte id = 0) { }
	}
}