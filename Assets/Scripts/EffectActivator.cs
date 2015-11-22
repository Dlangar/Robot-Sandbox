using System;
using System.Collections;
using UnityEngine;

public class EffectActivator : MonoBehaviour
{
   public enum EffectType
   {
      Animator,
      Audio,
      Particle,
      Generic
   }

   public GameObject Target;
   public EffectType Type;
   public float StartDelay;      // Delay in Seconds after master effect is started before this individual effect starts
   public float Duration;        // Duration in seconds this effect is allowed to play. If 0, this Effect is not manually cancelled

   [HideInInspector]
   public bool IsActive { get; private set; }
        
   public void Activate()
   {
      Invoke("StartEffect", StartDelay);
   }

   public void StartEffect()
   {
      switch (Type)
      {
         case EffectType.Animator:
            Animator animator = Target.GetComponent<Animator>();
            animator.enabled = true;
            break;
         case EffectType.Audio:
            AudioSource audio = Target.GetComponent<AudioSource>();
            audio.Play();
            break;
         case EffectType.Particle:
            ParticleSystem particleSystem = Target.GetComponent<ParticleSystem>();
            particleSystem.Play(true);
            break;
         case EffectType.Generic:
            Target.SetActive(true);
            break;
      }
      IsActive = true;
      // Do we have a Duration?
      if (Duration != 0.0f)
         Invoke("StopEffect", Duration);

   }

   public void StopEffect()
   {
      switch (Type)
      {
         case EffectType.Animator:
            Animator animator = Target.GetComponent<Animator>();
            animator.enabled = false;
            break;
         case EffectType.Audio:
            AudioSource audio = Target.GetComponent<AudioSource>();
            audio.Stop();
            break;
         case EffectType.Particle:
            ParticleSystem particleSystem = Target.GetComponent<ParticleSystem>();
            particleSystem.Stop(true);
            break;
         case EffectType.Generic:
            Target.SetActive(false);
            break;
      }
      IsActive = false;
   }
}


