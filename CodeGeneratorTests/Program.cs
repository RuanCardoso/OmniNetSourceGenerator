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
		[Client(1)]
		void RpcMethod() { }

		[Server(2)]
		private void RpcMethod2(DataBuffer buffer, NetworkPeer peer)
		{

		}
	}
}