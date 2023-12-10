using Omni.Core;

[Omni("SyncColor(0)", "SyncTest")]
[Omni("SyncColor2(0)", "SyncTest2")]
public partial class TesteClass
{
	static void Main()
	{
		Console.ReadKey();
	}

	protected override void SyncColor2_Client()
	{
		throw new NotImplementedException();
	}

	protected override void SyncColor2_Server()
	{
		throw new NotImplementedException();
	}

	protected override void SyncColor_Client()
	{
		throw new NotImplementedException();
	}

	protected override void SyncColor_Server()
	{
		throw new NotImplementedException();
	}

	protected override void SyncTest2_Client(DataIOHandler IOHandler, ushort fromId)
	{
		throw new NotImplementedException();
	}

	protected override void SyncTest2_Server(DataIOHandler IOHandler, ushort fromId)
	{
		throw new NotImplementedException();
	}

	protected override void SyncTest_Client(DataIOHandler IOHandler, ushort fromId)
	{
		throw new NotImplementedException();
	}

	protected override void SyncTest_Server(DataIOHandler IOHandler, ushort fromId)
	{
		throw new NotImplementedException();
	}
}

