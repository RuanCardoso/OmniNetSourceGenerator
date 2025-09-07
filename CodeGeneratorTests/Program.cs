using System;
using Omni.Core;


public partial class TestsRPCBaseeBase : NetworkBehaviour
{
	[NetworkVariable]
	private float m_Darma, m_Koty, m_Ritu;

	[NetworkVariable]
	private float m_Baka, m_Tyro;


	[Client]
	public void Modsdsve()
	{
		
	}

	[Client]
	void Jsdsump()
	{

	}

	[Client]
	void Atfdgtack()
	{

	}

	[Client]
	void Dehgfhgfhfend()
	{

	}
}

public partial class TestsRPCBase : TestsRPCBaseeBase
{

	[NetworkVariable]
	private float m_Vida, m_Mana;


	[Client]
	void Move()
	{
	}

	[Client]
	void Jump()
	{

	}

	[Client]
	void Attack()
	{

	}

	[Client]
	void Defend()
	{

	}
}

[GenerateSecureKeys]
public partial class TestsRPC : TestsRPCBase
{
	[NetworkVariable]
	private float m_Lama;

	[NetworkVariable(Target = Target.Self)]
	private float m_Kama;

	[NetworkVariable]
	private float m_Dama, m_REYT;

	[Client]
	void Sprint(int a)
	{

	}

	[Client]
	void Crouch()
	{

	}

	[Client]
	void Reload()
	{

	}

	[Client]
	void Interact()
	{

	}

	[Client]
	void OpenInventory()
	{

	}

	[Client]
	void UseItem()
	{

	}

	[Client]
	void CastSpell()
	{

	}

	[Client]
	void ChangeWeapon()
	{

	}

	[Client]
	void Heal()
	{

	}

	[Client]
	void BlockAttack()
	{

	}

	[Client]
	void Dodge()
	{

	}

	[Client]
	void ClimbWall()
	{

	}

	[Client]
	void SwimUnderwater()
	{

	}

	[Client]
	void DriveVehicle()
	{

	}

	[Client]
	void OpenDoor()
	{

	}

	[Client]
	void PickupObject()
	{

	}

	[Client]
	void ThrowGrenade()
	{

	}

	[Client]
	void AimWeapon()
	{

	}

	[Client]
	void ActivateSkill()
	{

	}

	[Client]
	void SendMessage()
	{

	}

	[Client]
	void ViewMap()
	{

	}

	[Client]
	void SaveProgress()
	{

	}

	[Client]
	void PauseGame()
	{

	}

	[Client]
	void ToggleFlashlight()
	{

	}

	[Client]
	void CheckHealth()
	{

	}

	[Client(200)]
	void RequestBackup()
	{

	}
}