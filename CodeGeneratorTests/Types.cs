namespace Omni.Core
{
	public struct RemoteStats
	{
	}

	public class DataIOHandler
	{
	}

	public class OmniAttribute : Attribute
	{
		public OmniAttribute(params string[] @params)
		{
		}
	}

	public class OmniObject : OmniDispatcher
	{

	}

	public class OmniDispatcher
	{
		protected virtual void Ovr()
		{

		}
	}

	public class MonoBehaviour
	{
		public int ID = 10;
	}
}