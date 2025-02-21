using Omni.Core;

#nullable disable

public partial class Program : NetworkBehaviour
{
	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable]
		private int m_Vida = 10;

		[Server(1)]
		private void RpcTest()
		{

		}
	}

	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable]
		private int m_Mana = 10;

		[Server(1)]
		private void RpcTest2()
		{

		}
	}
}