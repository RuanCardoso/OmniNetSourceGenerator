public partial class Test
{
	public static void Main()
	{
	}

	[Remote]
	partial void Move(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats);

	public override void MoveServerLogic(DataIOHandler IOHandler, ushort fromId, ushort toId, RemoteStats stats)
	{

	}
}

public struct RemoteStats
{
}

public class DataIOHandler
{
}

public class RemoteAttribute : Attribute
{

}