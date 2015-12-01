// #define DEBUG_PROJECTILE

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class WeaponProjectile: NetworkBehaviour
{
   // Our Effects & Parameters
   public float Speed = 300.0f;
   public LayerMask CollisionLayer;
   public WeaponEffect FlightEffect;
   public GameObject ImpactEffectPrefab;
   public float DespawnDelay = 0.0f;
   public float LookAheadDist = 2.0f;

   /// <summary>
   /// The mech that owns us
   /// </summary>
   Mech Owner;
   /// <summary>
   ///  The WeaponIndex of the Weapon that launches this projectile
   /// </summary>
   int WeaponIndex;
   Weapon OwnerWeapon;

   RaycastHit HitPoint;
   bool IsHit;
   bool PayloadDelivered = false;
   float DistTraveled = 0.0f;
   float DespawnTimer = 0.0f;
   bool Launched = false;

   /// <summary>
   /// Initialize all values
   /// </summary>
   public override void OnStartServer()
   {
      base.OnStartServer();
	}

   /// <summary>
   /// Even though a projectile is instantiated, it won't begin doining anything
   /// until the owning mech launches it. This establishes ownership, etc.
   /// </summary>
   /// <param name="ownerMech"></param>
   /// <param name="weaponIdx"></param>
   public void Launch(Mech ownerMech, int weaponIdx, Vector3 position, Quaternion rotation)
   {
      #if DEBUG_PROJECTILE
         Debug.Log(string.Format("Projectile from Weapon: {0} has launched!", WeaponIndex));
      #endif
      Owner = ownerMech;
      WeaponIndex = weaponIdx;
      OwnerWeapon = Owner.GetWeapon(WeaponIndex);
      transform.position = position;
      transform.rotation = rotation;
      IsHit = false;
      PayloadDelivered = false;
      DistTraveled = 0.0f;
      DespawnTimer = 0.0f;
      Launched = true;
   }

   /// <summary>
   /// Activate our effects!
   /// </summary>
   public override void OnStartClient()
   {
      base.OnStartClient();
      FlightEffect.Activate();
   }

   public override void OnNetworkDestroy()
   {
      base.OnNetworkDestroy();
      FlightEffect.Deactivate();
   }

	
	/// <summary>
   /// 
   /// </summary>
	void Update ()
   {
      if (!isServer)
         return;

      if (!Launched)
         return;

      if (IsHit)
      {
         #if DEBUG_PROJECTILE
            Debug.Log(string.Format("Projectile from Weapon: {0} has hit something.", WeaponIndex));
         #endif

         // Tell the Parent Weapon we hit something - but only once
         if (!PayloadDelivered)
         {
            Owner.DeliverWeaponPayload(WeaponIndex, HitPoint.transform.gameObject);
            #if DEBUG_PROJECTILE
               Debug.Log(string.Format("Projectile from Weapon: {0} has delivered its payload.", WeaponIndex));
            #endif
            PayloadDelivered = true;
         }

         // Removes the projectile, or waits until DelayTimer expires and then removes it
         DespawnProjectile();
         
      }

      // No collision occurred yet
      else
      {
         // Projectile step per frame based on velocity and time
         Vector3 step = transform.forward * Time.deltaTime * Speed;

         // Raycast for targets with ray length based on frame step by ray cast advance multiplier
         if (Physics.Raycast(transform.position, transform.forward, out HitPoint, step.magnitude * LookAheadDist, CollisionLayer))
         {
            // Tell the client to perform Impact..
            RpcActivateImpactEffect(HitPoint.point, HitPoint.normal);

            IsHit = true;
            DespawnTimer = 0.0f;
            return;
         }

         // Advances projectile forward
         transform.position += step;
         DistTraveled += step.magnitude;
         if (DistTraveled >= OwnerWeapon.Proto.Range)
         {
            #if DEBUG_PROJECTILE
               Debug.Log(string.Format("Projectile from Weapon: {0} has reached maximum range.", WeaponIndex));
            #endif
            // We haven't actually delivered a payload, but as far as the projectile is concerned, it's donw
            PayloadDelivered = true;
            IsHit = true;
         }
      }

   }

   void DespawnProjectile()
   {
      if (DespawnDelay > 0.0f)
      {
         DespawnTimer += Time.deltaTime;
         if (DespawnTimer < DespawnDelay)
            return;
      }

      // Destroy us!
      NetworkServer.Destroy(this.gameObject);
   }

   [ClientRpc]
   void RpcActivateImpactEffect(Vector3 location, Vector3 normal)
   {
      if (ImpactEffectPrefab != null)
      {
         // DLMTODO - We might need to adjust orientation of effects that have a direction, like bullet effects in the dirt. 
         Instantiate(ImpactEffectPrefab, location, Quaternion.LookRotation(normal));
      }
   }


}
