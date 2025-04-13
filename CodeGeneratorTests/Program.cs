
#nullable disable

using System;
using System.Collections.Generic;
using Omni.Core;

public partial class DebTyBase : NetworkBehaviour
{

}

public partial class PlayerBase : DebTyBase
{

}

public struct Vector3
{
	public float x { get; set; }
	public float y { get; set; }
	public float z { get; set; }
	public int[] array { get; set; }
}

namespace OmniNet
{
	[DeltaSerializable(Enabled = true)]
	public partial struct PlayerData
	{
		public int vida { get; set; }
		public Vector3 position { get; set; }
	}


	public partial class Player : PlayerBase
	{
		[NetworkVariable]
		private int m_Vida;

		[NetworkVariable]
		private Action<int, int, List<Player>> m_OnHealthChanged;

		[NetworkVariable]
		private ObservableDictionary<int, int> m_Dictionaryy;

		static void Main()
		{

		}

		void Test()
		{
			Vida = 10;
			// DelegateTest = null;
			SyncVida();
		}

		// partial void OnDelegateTestChanged(Func<int> prevDelegateTest, Func<int> nextDelegateTest, bool isWriting)
		// {
		// 	throw new NotImplementedException();
		// }

		partial void OnVidaChanged(int prevVida, int nextVida, bool isWriting)
		{
			throw new NotImplementedException();
		}
	}
}