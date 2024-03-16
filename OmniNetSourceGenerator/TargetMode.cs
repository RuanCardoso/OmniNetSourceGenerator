public enum TargetMode : byte
{
	/// <summary>
	/// Only the server will receive the data.
	/// </summary>
	Server = 0,
	/// <summary>
	/// All clients will receive the data.
	/// </summary>
	Broadcast = 1,
	/// <summary>
	/// All clients will receive the data, except the sender.
	/// </summary>
	BroadcastExcludingSelf = 2,
	/// <summary>
	/// Only the sender will receive the data.
	/// </summary>
	Self = 3,
}

public enum SerializerType : byte
{
	Json,
	MsgPack,
	MemPack,
}