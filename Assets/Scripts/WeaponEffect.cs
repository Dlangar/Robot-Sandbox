using UnityEngine;
using System.Collections;

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
      Dead
   }

   [SerializeField]
   protected Animator EffectAnimation;
   [SerializeField]
   protected Light EffectLight;
   [SerializeField]
   protected GameObject EffectMesh;
   [SerializeField]
   protected AudioSource EffectAudio;
   [SerializeField]
   protected ParticleSystem EffectParticles;

   public float Duration;        // Duration off effect in seconds. A duration of 0 means effect must be de-activated, or is de-activated on destroy time
   public float ExpireDelay;     // Provides a delay amount (in seconds) after the effect has been de-activated, for updates to still occur. This is so particles can finish, etc.
   public bool ActivateOnStart;  // If true, object will auto-activate when it becomes available

   float AccumTime;              // Accumulated Time, used for various phases
   Phase CurrentPhase;

   public virtual void Awake()
   {

   }

   public virtual void Start()
   {
      AccumTime = 0.0f;
      CurrentPhase = Phase.Inactive;
      if (ActivateOnStart)
         Activate();
   }

   public virtual void Update()
   {
      switch (CurrentPhase)
      {
         case Phase.Inactive:
         case Phase.Dead:
            return;
         case Phase.Active:
         {
            if (Duration > 0.0f)
            {
               AccumTime += Time.deltaTime;
               if (AccumTime >= Duration)
               {
                  Deactivate();
               }
            }
            break;
         }
         case Phase.Expiring:
         {
            AccumTime += Time.deltaTime;
            if (AccumTime >= ExpireDelay)
               Expire();
            break;
         }
      }
   }

   public virtual void Activate()
   {
      // Reset the timer
      AccumTime = 0.0f;

      // Enable the Animation
      if (EffectAnimation != null)
         EffectAnimation.enabled = true;
      // Play the Sound
      if (EffectAudio != null)
         EffectAudio.Play();
      // Activate the Light
      if (EffectLight != null)
         EffectLight.enabled = true;
      // Particles
      if (EffectParticles != null)
         EffectParticles.enableEmission = true;

      // And activate the object..
      EffectMesh.SetActive(true);

      CurrentPhase = Phase.Active;

   }

   public virtual void Deactivate()
   {
      EffectMesh.SetActive(false);

      // Turn off everything except particles
      if (EffectAnimation != null)
         EffectAnimation.enabled = false;
      if (EffectAudio != null && Duration == 0.0f)
         EffectAudio.Stop();
      if (EffectLight != null)
         EffectLight.enabled = false;


      // Particles
      if (ExpireDelay == 0.0f)
      {
         if (EffectParticles != null)
            EffectParticles.enableEmission = false;
         CurrentPhase = Phase.Dead;
      }
      else
      {
         AccumTime = 0.0f;
         CurrentPhase = Phase.Expiring;
      }

   }

   public void Expire()
   {
      if (EffectParticles != null)
         EffectParticles.enableEmission = false;
      CurrentPhase = Phase.Dead;
   }

}
