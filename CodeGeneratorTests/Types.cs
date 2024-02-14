namespace Omni.Core
{
	public class Remote: Attribute
	{
		public byte id;
		public string id2;
		public long id3;

		public Remote(byte id, string id2, long id3)
		{
			this.id = id;
			this.id2 = id2;
			this.id3 = id3;
		}
	}

	public class NetworkBehaviour
	{

	}
}