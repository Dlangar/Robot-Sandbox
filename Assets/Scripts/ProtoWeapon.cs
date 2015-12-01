using UnityEngine;
using System.Collections;

public class ProtoWeapon : MonoBehaviour
{
   // This is really just for display purposes, and doesn't have any real bearing
   // on the weapon's characteristics.
   public enum WeaponClass
   {
      Ballistic,     
      Beam,          
      Missile
   }

   public enum FireMethod
   {
      Trigger,    //  Fires once on trigger down
      Chain,      //  Starts firing when trigger goes down, stops firing when trigger goes up
      Lock,       //  Uses Missile Lock
   }

   // All beam weapons, and some ballistics will
   // use intant raycasts. All missiles, and some
   // ballistics may use projectiles
   public enum HitMethod
   {
      Instant,
      Projectile
   }

   public int ProtoID;
   public string DisplayName;       // Display Name of Weapon
   public WeaponClass Class;        // Weapon Class (ballistic, beam, missile)
   public FireMethod FireType;      // Firing Method
   public HitMethod HitType;        // Hit Type
   public float PrimaryDamage;      // Primary Damage amount at poi (point of impact)
   public float Range;              // Maximum Range of weapon.
   public float ChainDamageRate;    // If Chain weapon, rate at which primary damage is applied during chain
   public float SplashDamage;       // If weapon does AOE damage at poi, how much it does. This is in addition to Primary Damage
   public float SplashRadius;       // If weapon does AOE damage, the radius at which it looks for targets
   public float HeatGeneration;     // Heat generated on the firing unit each time the weapon fires
   public float HeatDamage;         // Heat delivered to target at poi (if any)
   public float Cooldown;           // Time (in seconds) before weapon can be used again when it is fired.
}
