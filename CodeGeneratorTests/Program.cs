#nullable disable

using OmniNet;

public partial class GenTests : NetworkBehaviour
{
	[LocalService]
	private GenTests GenService { get; set; }

	[NetworkVariable]
	private float m_Health = 100f;

	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class GenTests2 : NetworkBehaviour
	{
		[LocalService]
		private GenTests2 GenService { get; set; }

		[NetworkVariable]
		private float m_Health = 100f;
	}

	public class MonoBehaviour
	{

	}
}