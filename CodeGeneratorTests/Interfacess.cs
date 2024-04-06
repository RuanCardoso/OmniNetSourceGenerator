using System.Reflection.PortableExecutable;

namespace CodeGeneratorTests
{
	public class Carro
	{
		public void Andar()
		{

		}

		public void Dobrar()
		{

		}
	}


	public class Ferrari : Carro
	{

	}


	public class Lambo : Carro
	{

	}

	public class Camaro : Carro
	{
	}



	public abstract class IGateway
	{
		public abstract void Pay();
	}

	public class MercadoPago : IGateway
	{
		public override void Pay()
		{
			
		}
	}

	public class Stripe : IGateway
	{
		public override void Pay()
		{
			
		}
	}

	public class Main
	{

		IGateway gateway;

		public void Pagamento()
		{
			IGateway gateway;

			gateway = new Stripe();
		}
	}
}
