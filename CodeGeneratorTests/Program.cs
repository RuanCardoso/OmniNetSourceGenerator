using System.ComponentModel.DataAnnotations;
using Omni.Core;


public class BB
{
	public const int a = 102;
}

public partial class Player : NetworkBehaviour
{
	[Client(DeliveryMode = DeliveryMode.Sequenced, SequenceChannel = BB.a + 1)]
	public void SetNick2(Channel channel)
	{

	}


	[Client(SequenceChannel = BB.a, DeliveryMode = DeliveryMode.Sequenced)]
	public void SetNic3k2(Channel channel)
	{

	}


	[Server(Target = Target.Self, RequiresOwnership = false, SequenceChannel = BB.a - 29, DeliveryMode = DeliveryMode.Sequenced)]
	public void SetNick(Channel channel)
	{

	}



	[Server(Target = Target.Self, RequiresOwnership = true, SequenceChannel = BB.a + 5, DeliveryMode = DeliveryMode.Unreliable)]
	public void SetNisddck2(Channel channel)
	{

	}
}