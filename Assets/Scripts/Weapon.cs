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

   float FiringTime = 0.0f;
   float CooldownTime = 0.0f;
   float TimeSinceLastHitCheck = 0.0f;

	// Use this for initialization
	void Start ()
   {
      // Every weapon must have a protoweapon component
      Proto = GetComponent<ProtoWeapon>();

      // Note - not every weapon has to have an animator. Up to use code to check for null
      Animator = GetComponent<Animator>();

      // Reset the Weapon
      ResetWeapon();
	
	}

   // Resets Weapon State,
   // Resets all timers.
   public void ResetWeapon()
   {
      CurrentState = WeaponState.Ready;
      FiringTime = 0.0f;
      TimeSinceLastHitCheck = 0.0f;
      CooldownTime = 0.0f;
   }
	
	// Update is called once per frame
	void Update ()
   {
	   if (CurrentState == WeaponState.Firing)
      {
         FiringTime += Time.deltaTime;
         switch (Proto.FireType)
         {
            case ProtoWeapon.FireMethod.Trigger:
               PerformHitCheck();
               CeaseFire();
               break;
            case ProtoWeapon.FireMethod.Chain:
               TimeSinceLastHitCheck += Time.deltaTime;
               if (TimeSinceLastHitCheck >= Proto.ChainDamageRate)
               { 
                  PerformHitCheck();
                  TimeSinceLastHitCheck = 0.0f;
               }
               break;
            case ProtoWeapon.FireMethod.Lock:
               // DLMTODO Handle Missile Lock 
               break;
         }
      }
      if (CurrentState == WeaponState.OnCooldown)
      {
         CooldownTime += Time.deltaTime;
         if (CooldownTime >= Proto.Cooldown)
            ResetWeapon();
      }
	}

   /// <summary>
   /// In the case of Instant hit types, this does the raycast and determines if we hit. In the case
   /// of projectile types, this launches the projectile
   /// </summary>
   /// <returns></returns>
   public bool PerformHitCheck()
   {
      return false;
   }

   /// <summary>
   /// Begins the Firing Process.
   /// </summary>
   /// <returns>true if we successfully started firing. False if not</returns>
   public bool CommenceFire()
   {
      if (CurrentState == WeaponState.Firing || CurrentState == WeaponState.OnCooldown)
         return false;

      FiringTime = 0.0f;
      TimeSinceLastHitCheck = Proto.ChainDamageRate;
      CurrentState = WeaponState.Firing;
      return true;
   }

   public bool CeaseFire()
   {
      if (CurrentState != WeaponState.Firing)
         return false;

      // TODO Begin Cooldown Timer, put weapon on cooldown
      CurrentState = WeaponState.OnCooldown;
      return true;
      
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
