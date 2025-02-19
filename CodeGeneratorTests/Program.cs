using System;
using System.ComponentModel.Design;
using System.Numerics;
using Omni.Core;
using OmniNet;

#nullable disable

public partial class Program
{
	public static void Main(string[] args) { }
}

namespace OmniNet
{
	public class PlayerBaseRoot : Program
	{
        const byte idRpc = 1;

		[Server(idRpc)]
		public void RpcMethod() { }

		[Client(idRpc)]
		public void RpcMethod2() { }
	}
}