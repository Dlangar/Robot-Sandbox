using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// Mech
/// This class represents our primary interface into the Mech's simulation. Data values and attributes for the mech
/// come from the ProtoMech reference. Right now, we require the protodata component to be attached to the mech itself,
/// but we could easily change this to be a datadriven table, which is why that information lives on a separate component.
/// On Networking!
/// Here's the thing. You can't have network aware behaviors on child objects. So all network aware logic has to live in
/// this class, or in some other behavior that is attached to the root object off the player object. You can, based
/// on network conditions, then call particular functions on behaviors attached to child objects, but the network aware
/// code has to live here. This is what we had to do for our weapons. 
/// </summary>

[RequireComponent(typeof(ProtoMech))]
public class Mech : NetworkBehaviour
{
   Weapon[] Weapons;
   ProtoMech Proto;

   // Sync Vars controlling health and stuff
   [SyncVar]
   float CurrentHealth;
   [SyncVar]
   float CurrentSpeed;
   [SyncVar]
   float CurrentTurn;
   [SyncVar]
   float CurrentHeat;

   void Awake()
   {
      Proto = GetComponent<ProtoMech>();
      Weapons = GetComponentsInChildren<Weapon>();
      if (Weapons.Length != 2)
      {
         Debug.LogWarning("Found more or less than 2 weapons on mech.");
      }

      // Assign Each Weapon a unique Index to facility communications between
      // the Mech and its weapons
      for (int weapIdx = 0; weapIdx < Weapons.Length; weapIdx++)
         Weapons[weapIdx].WeaponIndex = weapIdx;
   }
   // Use this for initialization
   void Start ()
   {
	
	}
	
	// Update is called once per frame
	void Update ()
   {
	
	}

   public override void OnStartServer()
   {
      base.OnStartServer();
      Debug.Log(string.Format("Mech.OnStartServer: Player Object is available on server. IsServer: {0} Network Instance ID: {1}", isServer, netId.ToString()));

   }

   [Command]
   public void CmdCommenceFire(int weaponIdx)
   {
      Debug.Log(string.Format("Mech.CmdCommenceFire. WeaponIdx: {0}", weaponIdx));

      if (Weapons[weaponIdx] == null)
      {
         Debug.LogError(string.Format("No Weapon was found for Index: {0}", weaponIdx));
         return;
      }
      if (Weapons[weaponIdx].CommenceFire())
      {
         // Tell the Client to Commence Fire Effects
         RpcActivateFireEffect(weaponIdx);
      }
   }


   [ClientRpc]
   public void RpcActivateFireEffect(int weaponIdx)
   {
      Debug.Log(string.Format("Mech.RpcActivateFireEffect - Activating Effects for Weapon: {0}", weaponIdx));
      if (Weapons[weaponIdx] == null)
      {
         Debug.LogError(string.Format("No Weapon was found for Index: {0}", weaponIdx));
         return;
      }

      Weapons[weaponIdx].ActivateFireEffect();

   }

   /// <summary>
   /// Returns the requested Weapon associated with the mech
   /// </summary>
   /// <param name="weaponidx"></param>
   /// <returns></returns>
   public Weapon GetWeapon(int weaponIdx)
   {
      return Weapons[weaponIdx];
   }

   /// <summary>
   /// Because this is a network function, (it spawns network projectiles)
   /// it needs to live here, on the Network-aware Mech. 
   /// </summary>
   /// <param name="weaponIdx"></param>
   public void LaunchWeaponProjectile(int weaponIdx)
   {
      Weapon weapon = Weapons[weaponIdx];

      // Instantiate the Projectile and send it on its merry way
      GameObject projectileObj = (GameObject) Instantiate(weapon.ProjectilePrefab);
      projectileObj.GetComponent<WeaponProjectile>().Launch(this, weaponIdx, weapon.FireEffect.transform.position, weapon.FireEffect.transform.rotation);
      NetworkServer.Spawn(projectileObj);
   }

}
