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

   [HideInInspector]
   public ProtoWeapon Proto;
   Animator Animator;

   WeaponState CurrentState;

   // Our Effects
   public WeaponEffect FireEffect;
   public GameObject ImpactEffectPrefab;
   public GameObject ProjectilePrefab;

   /// <summary>
   /// The Owning Mech. The weapon assumes the mech component is at the root of it's hierarchy,
   /// and will look for this component there
   /// </summary>
   [HideInInspector]
   public Mech Owner;

   /// <summary>
   ///  This is a unique index given to this weapon
   ///  At runtime, to facilitate in communiation btween the weapon
   ///  and its owner
   /// </summary>
   [HideInInspector]
   public int WeaponIndex;


   float FiringTimer = 0.0f;
   float CooldownTimer = 0.0f;
   float LastHitCheckTimer = 0.0f;

	// Use this for initialization
	void Start ()
   {
      // Every weapon must have a protoweapon component
      Proto = GetComponent<ProtoWeapon>();

      // Note - not every weapon has to have an animator. Up to use code to check for null
      Animator = GetComponent<Animator>();

      Owner = transform.root.gameObject.GetComponent<Mech>();
      if (Owner == null)
         Debug.LogWarning("Weapon attached to a nonmech item. No owner, so it won't attribute damage correctly.");

      // Reset the Weapon
      ResetWeapon();
	
	}

   // Resets Weapon State,
   // Resets all timers.
   public void ResetWeapon()
   {
      CurrentState = WeaponState.Ready;
      FiringTimer = 0.0f;
      LastHitCheckTimer = 0.0f;
      CooldownTimer = 0.0f;
   }
	
	// Update is called once per frame
	void Update ()
   {
	   if (CurrentState == WeaponState.Firing)
      {
         FiringTimer += Time.deltaTime;
         switch (Proto.FireType)
         {
            case ProtoWeapon.FireMethod.Trigger:
               PerformHitCheck();
               CeaseFire();
               break;
            case ProtoWeapon.FireMethod.Chain:
               LastHitCheckTimer += Time.deltaTime;
               if (LastHitCheckTimer >= Proto.ChainDamageRate)
               { 
                  PerformHitCheck();
                  LastHitCheckTimer = 0.0f;
               }
               break;
            case ProtoWeapon.FireMethod.Lock:
               // DLMTODO Handle Missile Lock 
               break;
         }
      }
      if (CurrentState == WeaponState.OnCooldown)
      {
         CooldownTimer += Time.deltaTime;
         if (CooldownTimer >= Proto.Cooldown)
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
      if (Proto.HitType == ProtoWeapon.HitMethod.Projectile)
      {
         Debug.Log(string.Format("Launching projectile for weapon: {0}", WeaponIndex));
         Owner.LaunchWeaponProjectile(WeaponIndex);
      }
      // DLMTODO - Handle Instant Raycast
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

      FiringTimer = 0.0f;
      LastHitCheckTimer = Proto.ChainDamageRate;
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
