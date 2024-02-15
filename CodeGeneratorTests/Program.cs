
using Omni.Core;

//namespace a.a.e.d.aaaa
//{
	[Remote(Id = 5, Name = "SyncRot", Self = true)]
	[Remote(Id = 10, Name = "SyncMove", Self = false)]
	public partial class TesteClass : NetworkBehaviour
	{
	//[SyncVar]
	//private int teste;

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
//}
