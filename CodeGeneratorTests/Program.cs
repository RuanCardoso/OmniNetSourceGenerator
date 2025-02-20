using Omni.Core;

#nullable disable

public partial class Program : NetworkBehaviour
{
	[NetworkVariable]
	private int m_Ad = 10;
	public static void Main(string[] args) { }
}

namespace OmniNet
{

	[SkipCodeGen]
	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable]
		private int m_A2 = 10;

		void Start()
		{
			A = 2;
		}
	}

	// [SkipCodeGen]
	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable]
		private int m_A = 10;

		int b = 100;

		[Client(1)]
		void RpcMethod() { }

		[Server(2)]
		private void RpcMethod2(DataBuffer buffer, NetworkPeer peer, int id)
		{
			A = 1;
		}
	}
}

public partial class Vida : ClientBehaviour
{
	[NetworkVariable]
	private int m_Addd = 10;
}