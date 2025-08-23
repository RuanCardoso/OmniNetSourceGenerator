using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Numerics;
using Omni.Core;
using TestA;

public class Constants
{
	public const int MoveRpcId = 12;
	public const DeliveryMode MoveRpcDeliveryMode = DeliveryMode.Unreliable;
}

namespace TestA
{
	public partial class PlayerServer : NetworkBehaviour
	{

	}
}

namespace TestA
{

#pragma warning disable OMNI095
	[GenRpc("PlayerServer")]

	public partial class PlayerClient : NetworkBehaviour
	{

		const byte IdVar = 215;

#if DEBUG
#else
#endif

		[NetworkVariable(ServerBroadcastsClientUpdates = true, SequenceChannel = 14, Target = Target.GroupOthers)]
		private int m_Vida = 100;


#if DEBUG
		#region RPC
		#endregion

		[Client]
		void Test()
		{

		}

		[Server(1)]
		void Test2()
		{

		}

		[Server(2)]
		void Test24()
		{

		}

		[Server]
		void Test3()
		{
		}

#if DEBUG


		static void Main()
		{

		}
#endif
#endif

#if DEBUG


#endif



#if DEBUG
#elif DEBUG
#else

#endif

	}
}
