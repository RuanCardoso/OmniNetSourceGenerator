using Omni.Core;
using OmniNet;
using System;
using System.Collections.Generic;

public partial class Program : Player
{
	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public partial class PlayerBaseRoot : Program
	{
		[NetworkVariable(Id = 10)]
		private float m_Health = 100f;
	}

	public partial class PlayerBase : PlayerBaseRoot
	{
		[NetworkVariable(Id = 2, CheckEquality = true)]
		private float m_Mana = 100f;

		[NetworkVariable]
		private float m_Mana2, m_Mana3 = 100f;

		[NetworkVariable]
		private Dictionary<int, string> m_Dictionary = [];
	}

	public partial class Player : ServerBehaviour
	{
		[NetworkVariable(47)]
		private float m_AbsSpeed = 10f;

		[Server(10)]
		void OnMoveRpc()
		{
			
		}
	}

	public class MonoBehaviour
	{

	}
}