using System;

namespace Omni.Core
{
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
		public Server(byte id) { }
	}

	public class Client : Attribute
	{
		public Client(byte id) { }
	}

	public class NetworkVariable : Attribute
	{
		public NetworkVariable(bool track = false) { }

		public NetworkVariable(byte id) { }
	}
}