#define DEBUG_MECH

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
   public enum MechState
   {
      Inactive,
      Spawning,
      Alive,
      Dying,
      Dead,
      Despawning
   }

   Weapon[] Weapons;
   ProtoMech Proto;
   MechController Controller;

   // Sync Vars controlling health and stuff
   [SyncVar]
   float CurrentHealth;
   [SyncVar]
   float CurrentSpeed;
   [SyncVar]
   float CurrentTurn;
   [SyncVar]
   float CurrentHeat;
   // On CurrentState
   // We're currently tying quite a bit of functionality directly to the mechstate. ie., only damaged in this state, only controllable in that state, etc.
   // These conditions are probably best served as a separe mask or collection flags, that the states could then adjust, but so could other factors.
   // But for now, let's not make this more complicated than it has to be. 
   // So here are the rules currently:
   // Only Alive mechs can be damaged, or have their heat adjusted
   // Only Alive mechs can be controlled by the player. During spawn, dying, or death, the player can't control the mech.
   [HideInInspector]
   [SyncVar]
   public MechState CurrentState;

   void Awake()
   {
      Proto = GetComponent<ProtoMech>();
      Controller = GetComponent<MechController>();
      Weapons = GetComponentsInChildren<Weapon>();
      if (Weapons.Length != 2)
      {
         Debug.LogWarning("Found more or less than 2 weapons on mech.");
      }

      // Assign Each Weapon a unique Index to facility communications between
      // the Mech and its weapons
      for (int weapIdx = 0; weapIdx < Weapons.Length; weapIdx++)
         Weapons[weapIdx].WeaponIndex = weapIdx;

      if (isServer)
      {
         CurrentState = MechState.Inactive;
      }
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
      #if DEBUG_MECH
         Debug.Log(string.Format("Mech.OnStartServer: Player Object is available on server. IsServer: {0} Network Instance ID: {1}", isServer, netId.ToString()));
      #endif

      // Initialize our Health Stats
      CurrentHealth = Proto.MaxHealth;
      CurrentHeat = 0.0f;
      CurrentSpeed = Proto.MaxSpeed;
      CurrentTurn = Proto.MaxTurn;

      // Set State as Spawning
      SetState(MechState.Spawning);
   }

   /// <summary>
   /// SetState - The interesting things happend when we make state transitions, so we 
   /// wrap all that functionality up here. While setting the mech's state can result
   /// in actions being taken on the client, only the server should be modifying the state
   /// </summary>
   /// <param name="newState"></param>
   [Server]
   public void SetState(MechState newState)
   {
      // If an individual state wants to recursively move to another state, we set tha flag, the next state, and do so.
      MechState nextState = newState;
      bool recurse = false;

      // Any Attempt to set a state that is already active is thrown out
      if (newState == CurrentState)
         return;

      #if DEBUG_MECH
         Debug.LogFormat("Mech {0} setting State: {1}", netId.ToString(), newState);
      #endif
      switch(newState)
      {
         case MechState.Spawning:
            Debug.LogFormat("Mech {0} has Spawned!", netId.ToString());
            recurse = true;
            nextState = MechState.Alive;
            break;
         case MechState.Alive:
            break;
         case MechState.Dying:
            Debug.LogFormat("Mech {0} is Dying!", netId.ToString());
            // And Play Death Effects on the client
            RpcActivateDeathEffect();
            // In 5 seconds, really die
            Invoke("SetDead", 5.0f);
            break;
         case MechState.Dead:
            // Eventually we probably want to destroy this object here. But for now I think it's fun to just leave the mech on the battlefield.
            Respawn();
            break;
         default:
            Debug.LogWarning("Unhandled State: " + newState);
            break;
      }
      // Set Current State
      CurrentState = newState;

      // And if we're setting state again, do so!
      if (recurse)
         SetState(nextState);

   }

   /// <summary>
   /// 
   /// </summary>
   void SetDead()
   {
      SetState(MechState.Dead);
   }

   /// <summary>
   /// Respawn
   /// Creates a new instance of a playerprefab, and associates this player with that instance.
   /// </summary>
   [Server]
   void Respawn()
   {
      #if DEBUG_MECH
         Debug.LogFormat("Respawning Player associated with Mech: {0}", netId.ToString());
      #endif
      GameObject playerPrefab = NetworkManager.singleton.playerPrefab;
      Transform startPosition = NetworkManager.singleton.GetStartPosition();
      GameObject newPlayerObj = (GameObject)Instantiate(playerPrefab, startPosition.position, startPosition.rotation);
      NetworkServer.ReplacePlayerForConnection(connectionToClient, newPlayerObj, playerControllerId);
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

   [ClientRpc]
   public void RpcActivateDeathEffect()
   {
      Debug.Log("BOOM! Mech Blew up!");
      // Trigger falling animation on the server - this will get replicated to client
      Controller.FallForward();
      return;
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

   /// <summary>
   /// DeliverWeaponPayload
   /// A weapon has it the mech, or a projectile fired by a weapon. We have the WeaponIDX, so use
   /// that to find the protodata for the weapon to get the damage to deal (or whatever)
   /// NOTE - This is called on the weapon that is DELIVERING the damage. It will determine
   /// what other mechs might have been damaged or what not.
   /// </summary>
   /// <param name="weaponIdx"></param>
   public void DeliverWeaponPayload(int weaponIdx, GameObject hitObject)
   {
      Weapon weapon = Weapons[weaponIdx];
      ProtoWeapon protoWeapon = weapon.Proto;

      // Did we hit a mech?
      Mech targetMech = hitObject.GetComponent<Mech>();
      if (targetMech != null)
      {
         targetMech.ApplyDamage(protoWeapon.PrimaryDamage);
         targetMech.ApplyHeat(protoWeapon.HeatDamage);
      }

      // Check for AOE damage
      if (protoWeapon.SplashDamage> 0.0f)
      {
         Mech[] allMechs = FindObjectsOfType<Mech>() as Mech[];
         foreach(Mech mech in allMechs)
         {
            if (mech == this)
               continue;
            float dist = (mech.transform.position - transform.position).magnitude;
            if (dist <= protoWeapon.SplashRadius)
            {
               mech.ApplyDamage(protoWeapon.SplashDamage);
               mech.ApplyHeat(protoWeapon.HeatDamage);
            }
         }
      }
   }

   /// <summary>
   /// ApplyDamage
   /// Eventually, we could do all kinds of crazy things here with Damage types, armor, defenses, etc.
   /// For now, we apply damage. Positive damage reduces current health, negative damage increases current health.
   /// </summary>
   /// <param name="damageValue"></param>
   public void ApplyDamage(float damageValue)
   {
      // Only alive mechs can take damage
      if (CurrentState != MechState.Alive)
         return;
      #if DEBUG_MECH
         Debug.LogFormat("Mech {0} has taken {1} points of damage! Current Health: {2}", netId.ToString(), damageValue, CurrentHealth);
      #endif
      float AdjustedHealth = CurrentHealth - damageValue;
      if (AdjustedHealth < 0.0f)
      {
         CurrentHealth = 0.0f;
         SetState(MechState.Dying);
      }
      if (AdjustedHealth > Proto.MaxHealth)
         AdjustedHealth = Proto.MaxHealth;

      CurrentHealth = AdjustedHealth;
   }

   /// <summary>
   /// Mechs start off at 0 heat.
   /// </summary>
   /// <param name="heatValue"></param>
   public void ApplyHeat(float heatValue)
   {
      // Only alive mechs can overheat..
      if (CurrentState != MechState.Alive)
         return;
#if DEBUG_MECH
      Debug.LogFormat("Mech {0} has heated up by {1} units! CurrentHeat: {2}", netId.ToString(), heatValue, CurrentHeat);
#endif

      float AdjustedHeat = CurrentHeat + heatValue;
      // I'm not sure yet what Overheated is. Is  it a condition? A mechstate? A damageState? I'll need to think
      // about this..
      if (AdjustedHeat >= Proto.MaxHeat)
      {
         CurrentHeat = Proto.MaxHeat;
#if DEBUG_MECH
         Debug.LogFormat("Mech {0} has overheated!", netId.ToString());
#endif
         return;
      }
      if (AdjustedHeat < 0.0f)
         AdjustedHeat = 0.0f;
      // Any time we're applying heat and we were cool, then initiate the heat dissipation coroutine
      if (CurrentHeat == 0.0f && AdjustedHeat > 0.0f)
      {
         CurrentHeat = AdjustedHeat;
         StopCoroutine(DissipateHeat());
         StartCoroutine(DissipateHeat());
         return;
      }

      CurrentHeat = AdjustedHeat;
   }

   /// <summary>
   /// Dissipates Heat from the mech over time. If Heat reaches 0, exits coroutine..
   /// </summary>
   /// <returns></returns>
   IEnumerator DissipateHeat()
   {
      while (CurrentHeat > 0.0f)
      {
         CurrentHeat -= Proto.CoolingRate;
         if (CurrentHeat < 0.0f)
            CurrentHeat = 0.0f;
         #if DEBUG_MECH
            Debug.LogFormat("Mech {0} has cooled by {1} units!", netId.ToString(), Proto.CoolingRate);
         #endif

         yield return new WaitForSeconds(1.0f);
      }
      #if DEBUG_MECH
         Debug.LogFormat("Mech {0} has cooled off!", netId.ToString());
      #endif

   }

}
