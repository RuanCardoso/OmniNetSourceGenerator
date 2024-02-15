using Azeth;
using Omni.Core;

namespace Programa.Zeth
{
	enum aa
	{
		a
	}

	class bb
	{

	}

	struct vv
	{
		string a;
		//bb a2;
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

		[NetVar]
		private Player teste6 = new();

		[NetVar]
		private vv[] teste62 = new vv[1];

		[NetVar]
		private Player[] teste162 = Array.Empty<Player>();

		[NetVar]
		private List<vv> teste621 = new();

		[NetVar]
		private List<Player> testea621 = new();

		[NetVar]
		private Dictionary<int, Player> testea62a1 = new();

		private static void Main()
		{

		}

		protected override bool OnPropertyChanged(string netVarName, int id)
		{
			Console.WriteLine(netVarName);
			return true;
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
