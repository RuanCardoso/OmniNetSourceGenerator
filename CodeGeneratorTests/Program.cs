using Omni.Core;

public partial class Program : NetworkBehaviour
{
	[NetworkVariable]
	private float m_Aber = 100f;

	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable]
		private float m_Aber = 100f;
	}


	public partial class PlayerBase : NetworkBehaviour
	{
		[NetworkVariable]
		private float m_Mana = 100f;

		[NetworkVariable]
		private float m_Mana2 = 100f;
	}

	public partial class Player : PlayerBase
	{
		[NetworkVariable]
		private float m_Health = 100f;

		[NetworkVariable]
		private float m_Health2 = 100f;

		[NetworkVariable]
		private float m_Health4 = 100f;

		[LocalService]
		private PlayerBase m_Gen;

		void Test()
		{

		}

		[Server(4)]
		void OnMove()
		{

		}
	}

	public class MonoBehaviour
	{

	}
}