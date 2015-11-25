using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WeaponEffect
/// WeaponEffect is our effects package. It can be attached to a weapon, a ballistic, used for impacts. Basically anywhere we need a collection off
/// effects put together. This base class is going to provide some basic functionality, but it is expecte that this class will be overridden to
/// create many kinds of effects.
/// It should be noted, WeaponEffects are intended to be placed on objects that are either instantiated on the client only, or 
/// live only activated on the client. 
/// </summary>
public class WeaponEffect : MonoBehaviour
{
   public enum Phase
   {
      Inactive,
      Active,
      Expiring,
   }

   public float Lifespan;        // Overall lifespan of event. If event has 0 as a lifespan, then it must be manually deactivated.
   public bool ActivateOnStart;  // If true, object will auto-activate when it becomes available
   public bool DestroyOnDeactivate;  // If true, this object will destroy itself when it is deactivated.

   Phase CurrentPhase;
   EffectActivator[] EffectsList;

   public virtual void Awake()
   {
      EffectsList = GetComponentsInChildren<EffectActivator>(true);
   }

   public virtual void Start()
   {
      CurrentPhase = Phase.Inactive;
      if (ActivateOnStart)
         Activate();
   }

   public virtual void Update()
   {
   }

   public virtual void Activate()
   {
      CurrentPhase = Phase.Active;
      foreach (EffectActivator effect in EffectsList)
      {
         effect.Activate();
      }
      if (Lifespan != 0.0f)
         Invoke("Deactivate", Lifespan);
   }

   public virtual void Deactivate()
   {
      CurrentPhase = Phase.Inactive;
      foreach (EffectActivator effect in EffectsList)
      {
         // Any Effects that are still active will be stopped
         if (effect.IsActive)
            effect.StopEffect();       
      };

      if (DestroyOnDeactivate)
         Destroy(gameObject);
   }

}
