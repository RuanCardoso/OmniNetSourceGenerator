using System.Data.Common;
using System.Numerics;
using Omni.Core;

public class Constants
{
	public const int MoveRpcId = 12;
	public const DeliveryMode MoveRpcDeliveryMode = DeliveryMode.Unreliable;
}



[GenRpc($"{nameof(PlayerClient)}")]
public partial class PlayerServer : ServerBehaviour
{
	[Server(5, SequenceChannel = 10, DeliveryMode = Constants.MoveRpcDeliveryMode)]
	protected void Move(int vida, int calor)
	{

	}
}

[GenerateSecureKeys]
[GenRpc($"{nameof(PlayerServer)}")]
public partial class PlayerClient : ClientBehaviour
{
	[NetworkVariable(Target = Target.All, DeliveryMode = DeliveryMode.Unreliable, ServerBroadcastsClientUpdates = true)]
	private int m_Life2;

	[NetworkVariable(Target = Target.All, DeliveryMode = DeliveryMode.Unreliable, ServerBroadcastsClientUpdates = true)]
	private ObservableDictionary<int, int> m_Inventory;

	[Client(5 + 10, SequenceChannel = 10, DeliveryMode = Constants.MoveRpcDeliveryMode)]
	private void Move()
	{

	}

	static void Main()
	{

	}
}
