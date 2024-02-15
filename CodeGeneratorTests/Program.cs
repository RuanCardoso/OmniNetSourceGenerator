using Azeth;
using Omni.Core;

namespace a.a.e.d.aaaa
{
	enum aa {
		a
	}

	class bb
	{

	}

	struct vv
	{

	}

	[Remote(Id = 5, Name = "SyncRot", Self = true)]
	[Remote(Id = 10, Name = "SyncMove", Self = true)]
	public partial class TesteClass : NetworkBehaviour
	{
		[NetVar]
		private int teste = 100;

		[NetVar]
		private string teste2 = "100";

		[NetVar]
		private aa teste3 = aa.a;

		[NetVar]
		private bb teste4 = new();

		[NetVar]
		private vv teste5 = new();
		
		//[NetVar]
		//private Player teste6 = new();

		private static void Main()
		{

		}

		partial void SyncRot(IDataReader reader, NetworkPeer peer)
		{
			throw new NotImplementedException();
		}

		void Test()
		{
			
		}
	}
}
