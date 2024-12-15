using Omni.Core;
using System;
using System.Collections.Generic;

public partial class Program : ServerBehaviour
{
	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable]
		private ObservableDictionary<int, int> m_Aber = new();

		[NetworkVariable]
		private float m_AA = 100f;

		[NetworkVariable]
		private float m_DDD = 100f;

		void a()
		{
			
		}
	}


	public partial class PlayerBase : NetworkBehaviour
	{
		[NetworkVariable]
		private float m_Mana = 100f;

		[NetworkVariable]
		private float m_Mana2 = 100f;
	}

	public partial class Player : ServerBehaviour
	{
		[Server(10)]
		void OnMoveRpc()
		{
			// invocado no servidor
		}
	}

	public class MonoBehaviour
	{

	}
}