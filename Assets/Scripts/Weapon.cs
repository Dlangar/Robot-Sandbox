using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ProtoWeapon))]
public class Weapon : MonoBehaviour
{
   public enum WeaponState
   {
      Ready,
      Firing,
      OnCooldown
   }

   ProtoWeapon Proto;
   Animator Animator;

   WeaponState CurrentState;

   // Our Effects
   [SerializeField]
   WeaponEffect FireEffect;
   [SerializeField]
   GameObject ImpactEffectPrefab;


	// Use this for initialization
	void Start ()
   {
      // Every weapon must have a protoweapon component
      Proto = GetComponent<ProtoWeapon>();

      // Note - not every weapon has to have an animator. Up to use code to check for null
      Animator = GetComponent<Animator>();

      CurrentState = WeaponState.Ready;

	
	}
	
	// Update is called once per frame
	void Update ()
   {
	   if (CurrentState == WeaponState.Firing)
      {
         // TODO - Use fire rates, and verify that 

         // Then stop firing
         if (Proto.FireType == ProtoWeapon.FireMethod.Trigger)
            CeaseFire();
      }
	}

   public void CommenceFire()
   {

      if (CurrentState == WeaponState.Firing || CurrentState == WeaponState.OnCooldown)
         return;

      CurrentState = WeaponState.Firing;

   }

   public void CeaseFire()
   {
      if (CurrentState != WeaponState.Firing)
         return;

      // TODO Begin Cooldown Timer, put weapon on cooldown
      CurrentState = WeaponState.Ready;      
      
   }

   // DLM TODO - See if we hit something
   void DoHitCheck()
   {

   }

   public void ActivateFireEffect()
   {
      Debug.Log("Activating Fire Effect on Client..");
      if (FireEffect != null)
         FireEffect.Activate();
   }

   void DeactivateFireEffect()
   {
      FireEffect.Deactivate();
   }

}
