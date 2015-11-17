using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.CrossPlatformInput;

/// <summary>
/// MechUserControl
/// A control scheme based on ThirdPersonUserControl that takes
/// advantage of the StandardAssets Universal Input Mgr
/// </summary>

[RequireComponent(typeof (MechController))]
public class MechUserControl : NetworkBehaviour
{
   private MechController m_Character; // A reference to the ThirdPersonCharacter on the object
   private Transform m_Cam;                  // A reference to the main camera in the scenes transform
   private Vector3 m_CamForward;             // The current forward direction of the camera
   private Vector3 m_Move;
   private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
   


   /// <summary>
   /// Start
   /// Pretty much everything that we do in MechUserControl we 
   /// only do for the local player..
   /// </summary>
   private void Start()
   {
      if (!isLocalPlayer)
         return;

      // get the transform of the main camera
      if (Camera.main != null)
      {
         m_Cam = Camera.main.transform;
      }
      else
      {
         Debug.LogWarning(
               "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.");
         // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
      }

      // get the third person character ( this should never be null due to require component )
      m_Character = GetComponent<MechController>();
   }


   private void Update()
   {
      if (!isLocalPlayer)
         return;

      if (!m_Jump)
      {
         m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
      }
   }


   // Fixed update is called in sync with physics
   private void FixedUpdate()
   {
      if (!isLocalPlayer)
         return;

      // read inputs
      float h = CrossPlatformInputManager.GetAxis("Horizontal");
      float turret_yaw = CrossPlatformInputManager.GetAxis("Mouse X");
      float turret_pitch = CrossPlatformInputManager.GetAxis("Mouse Y");
      float thrust = CrossPlatformInputManager.GetAxis("Thrust");

      // Debug.Log(string.Format("Turret Yaw & Pitch: {0}, {1} Thrust: {2}", turret_yaw, turret_pitch, thrust));

      // calculate move direction to pass to character
      if (m_Cam != null)
      {
         // calculate camera relative direction to move:
         m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
         m_Move = m_CamForward + h*m_Cam.right;
      }
      else
      {
         // we use world-relative directions in the case of no main camera
         m_Move = Vector3.forward + h*Vector3.right;
      }

      // pass all parameters to the character control script
      m_Character.Move(m_Move, thrust, turret_yaw, turret_pitch, m_Jump);
      m_Jump = false;
   }

}
