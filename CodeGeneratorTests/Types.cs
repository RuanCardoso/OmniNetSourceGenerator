namespace Omni.Core
{
	public struct RemoteStats
	{
	}

	public class DataIOHandler
	{
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OmniAttribute : Attribute
	{
		public OmniAttribute(params string[] @params)
		{
		}
	}

	public class RemoteAttribute : Attribute
	{
		public RemoteAttribute(byte id)
		{
		}
	}

	public class OmniObject : OmniDispatcher
	{
		public bool IsServer { get; }
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