using Omni.Core;

#nullable disable

public partial class Program : DualBehaviour
{
	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class PlayerBaseRoot : Program
	{
		int b = 100;

		[Client(1)]
		void RpcMethod() { }

		[Server(2)]
		private void RpcMethod2(DataBuffer buffer, NetworkPeer peer)
		{
			
		}
	}
}