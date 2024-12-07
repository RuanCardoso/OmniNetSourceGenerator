using Omni.Core;
public partial class GenTests : NetworkBehaviour
{
	[GlobalService]
	private GenTests? GenService { get; set; } = null;

	[NetworkVariable]
	private float m_Health = 100f;

	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class GenTests2 : NetworkBehaviour
	{
		[LocalService]
		private GenTests2? GenService { get; set; } = null;

		[NetworkVariable(21)]
		private float m_Health = 100f;

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