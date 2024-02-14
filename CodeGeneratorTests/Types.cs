namespace Omni.Core
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class Remote : Attribute
	{
		public byte Id { get; set; }
		public string? Name { get; set; } // Roslyn Analyzer
		public bool Self { get; set; } // Roslyn Analyzer
	}

	public class NetworkBehaviour
	{
		public bool IsServer;
	}

	public class NetworkPeer
	{
		
	}

	public class IDataReader
	{

	}
}