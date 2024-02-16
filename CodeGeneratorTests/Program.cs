using Azeth;
using Omni.Core;
using System.Diagnostics.CodeAnalysis;

namespace Programa.Zeth
{
	enum aa
	{
		a
	}

	class bb
	{

	}

	struct vv : IEquatable<vv>, IEqualityComparer<vv>
	{
		public bool Equals(vv other)
		{
			throw new NotImplementedException();
		}

		public bool Equals(vv x, vv y)
		{
			throw new NotImplementedException();
		}

		public int GetHashCode([DisallowNull] vv obj)
		{
			throw new NotImplementedException();
		}
	}

	class Equips : Dictionary<string, string>
	{

	}

	[Remote(Id = 5, Name = "SyncRot", Self = true)]
	[Remote(Id = 10, Name = "SyncMovee", Self = true)]
	public partial class TesteClass : NetworkBehaviour
	{
		[NetVar(SerializeAsJson = true, VerifyIfEqual = true)]
		private int test1be, ka2ba, as223 = 100;

		[NetVar]
		private string tests21e2 = "100";

		[NetVar]
		private aa testbse23;

		[NetVar]
		private bb tessd4;

		[NetVar]
		private vv testeww221e5 = new();

		[NetVar]
		private Player te2eee6 = new();

		[NetVar]
		private vv[] testbve62;

		[NetVar]
		private Player[] tetesra162;

		[NetVar(VerifyIfEqual = false)]
		private List<vv> tesdfte32s621 = new();

		[NetVar(SerializeAsJson = true)]
		private List<Player> tessstes621;

		[NetVar]
		private List<Dictionary<int, Player>> tesaastea62a1 = new();

		[NetVar(SerializeAsJson = true)]
		private Dictionary<int, Player> disati;

		[NetVar(VerifyIfEqual = true)]
		private Equips di43ti20 = new();

		[NetVar(SerializeAsJson = true)]
		private HashSet<int> aa3s34d = new();

		private static void Main()
		{

		}

		protected override bool OnPropertyChanged(string netVarName, int id)
		{
			Console.WriteLine(netVarName);
			return true;
		}

		void Test()
		{

		}
	}
}
