using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.Cameras;

/// <summary>
/// MechController
/// Our heavily modified controller, originally based on ThirdPersonController from Unity Standard Assets
/// </summary>

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class MechController : NetworkBehaviour
{

	[SerializeField] float m_MovingTurnSpeed = 360;
	[SerializeField] float m_StationaryTurnSpeed = 0;
   [SerializeField] float m_ForwardAccel = 1;

	[SerializeField] float m_JumpPower = 12f;
	[Range(1f, 4f)][SerializeField] float m_GravityMultiplier = 2f;
	[SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
   
   [SerializeField] float m_MoveSpeedMultiplier = 1f;
	[SerializeField] float m_AnimSpeedMultiplier = 1f;

	[SerializeField] float m_GroundCheckDistance = 0.1f;

   [Header("Turret Control")]
   public bool EnableTurretControl = true;
   public bool AutoCentering = false;
   public GameObject TurretObj;
   public Vector2 TurretRotationRange = new Vector3(70, 70);
   public float TurretRotationSpeed = 10;
   public float TurretDampingTime = 0.2f;
   public float TurretRecenteringTime = 0.5f;
   [Space(10)]

   Rigidbody m_Rigidbody;
	Animator m_Animator;
   AudioSource m_EngineSound;
   AudioSource m_FootstepSound;
   AudioSource m_TurretSound;

   // Turret Rotation
   Vector3 m_TurretTargetAngles;
   Vector3 m_TurretFollowAngles;
   Vector3 m_TurretFollowVelocity;
   float m_TurretYaw;
   float m_TurretPitch;
   Quaternion m_TurretOriginalRotation;

   float m_ForwardAmount;
   float m_TurnAmount;

   bool m_IsGrounded;
	float m_OrigGroundCheckDistance;
	const float k_Half = 0.5f;
	Vector3 m_GroundNormal;
	float m_CapsuleHeight;
	Vector3 m_CapsuleCenter;
	CapsuleCollider m_Capsule;
   float m_AnimWalkSpeed;
   Vector3 m_CurrentMove;


	void Start()
	{
		m_Animator = GetComponent<Animator>();
		m_Rigidbody = GetComponent<Rigidbody>();
		m_Capsule = GetComponent<CapsuleCollider>();

      m_EngineSound = gameObject.transform.FindChild("EngineAudio").gameObject.GetComponent<AudioSource>();
      m_FootstepSound = gameObject.transform.FindChild("FootstepAudio").gameObject.GetComponent<AudioSource>();
      m_TurretSound = gameObject.transform.FindChild("TurretAudio").gameObject.GetComponent<AudioSource>();

      m_CapsuleHeight = m_Capsule.height;
		m_CapsuleCenter = m_Capsule.center;

		m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
		m_OrigGroundCheckDistance = m_GroundCheckDistance;

      m_TurretOriginalRotation = TurretObj.transform.localRotation;

   }

   /// <summary>
   /// Update
   /// Make sure the pitch is updated for both local and non local clients by
   /// placing it here
   /// </summary>
   void Update()
   {
      // Update the Engine Sound based on our object's velocity
      float speed = m_Animator.GetFloat("Forward");
      m_EngineSound.pitch = Mathf.Max(0.15f, Mathf.Abs(speed));

      // And the turret rotation amounds to update the turret sound
      float turret_x = m_Animator.GetFloat("TurretPitch");
      float turret_y = m_Animator.GetFloat("TurretYaw");
      float turretRotAmt = new Vector2(turret_x, turret_y).magnitude;
      //m_TurretSound.pitch = Mathf.Lerp(m_TurretSound.pitch, turretRotAmt, Time.deltaTime);
      m_TurretSound.pitch = Mathf.Clamp(turretRotAmt, 0.0f, 1.0f);

   }

   /// <summary>
   /// FixedUpdate
   /// Gather the turret rotation parameters from the  animator, and rotate the turret
   /// </summary>
   void FixedUpdate()
   {
      float turretYaw = m_Animator.GetFloat("TurretYaw");
      float turretPitch = m_Animator.GetFloat("TurretPitch");
      HandleTurretRotation(turretYaw, turretPitch);
   }

   public override void OnStartLocalPlayer()
   {
      base.OnStartLocalPlayer();
      // Find the Camera..
      MechCamera mechCam = GameObject.FindObjectOfType<MechCamera>();
      if (mechCam == null)
      {
         Debug.LogError("Unable to locate MechCamera in the scene to attach to this player.");
         return;
      }

      // Set the Camera Obj's rotater to our turret, and the 
      // target object to our object..
      //if (TurretObj != null)
      //   mechCam.SetRotateObj(TurretObj.transform);
      mechCam.SetRotateObj(gameObject.transform);
      mechCam.SetTarget(gameObject.transform);

   }


   /// <summary>
   /// This is called by the UserInput system, which updates our internal variables
   /// based on the move vector passed in, and updates the animator. User Input is only
   /// taken for local players.  However, this could eventually also be used
   /// by an AI system.
   /// IMPORTANT - This function is called from UserInput.Update, which only
   /// gets called on the local client. So EVERYTHING in this function
   /// happens ONLY on the local client.
   /// </summary>
   /// <param name="move">World-space vector indicating the direction the mech would like to move</param>
   /// <param name="turretYaw">-1 to 1 value indicating desired turret rotation about the Y axis</param>
   /// <param name="turretPitch">-1 to 1 value indicating desired turret rotation about the X axis</param>
   /// <param name="jump"></param>
   public void Move(Vector3 move, float thrust, float turretYaw, float turretPitch, bool jump)
	{
		// convert the world relative moveInput vector into a local-relative
		// turn amount and forward amount required to head in the desired
		// direction.
		if (move.magnitude > 1f)
         move.Normalize();

      if (Mathf.Abs(thrust) < 0.15f)
         thrust = 0.0f;

      // Convert move into local space
		move = transform.InverseTransformDirection(move);
		CheckGroundStatus();

		move = Vector3.ProjectOnPlane(move, m_GroundNormal);
      m_CurrentMove = move;

      if (move.z < 0.0f)
      {
         m_TurnAmount = Mathf.Atan2(move.x, -move.z);
      }
      else
      {
         m_TurnAmount = Mathf.Atan2(move.x, move.z);
      }

      //m_ForwardAmount = Mathf.Lerp(m_ForwardAmount, thrust, (Time.deltaTime * m_ForwardAccel));
      m_ForwardAmount = thrust;

		// control and velocity handling is different when grounded and airborne:
		if (m_IsGrounded)
		{
			HandleGroundedMovement(jump);
		}
		else
		{
			HandleAirborneMovement();
		}

		// send input and other state parameters to the animator
		UpdateAnimator(move, turretYaw, turretPitch);

	}


	void UpdateAnimator(Vector3 move, float turretYaw, float turretPitch)
	{
		// update the animator parameters
		m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
		m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
      m_Animator.SetFloat("TurretYaw", turretYaw, 0.1f, Time.deltaTime);
      m_Animator.SetFloat("TurretPitch", turretPitch, 0.1f, Time.deltaTime);

      m_Animator.SetBool("OnGround", m_IsGrounded);
		if (!m_IsGrounded)
		{
			m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
		}

		// calculate which leg is behind, so as to leave that leg trailing in the jump animation
		// (This code is reliant on the specific run cycle offset in our animations,
		// and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
		float runCycle =
			Mathf.Repeat(
				m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
		float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
		if (m_IsGrounded)
		{
			m_Animator.SetFloat("JumpLeg", jumpLeg);
		}

		// the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
		// which affects the movement speed because of the root motion.
		if (m_IsGrounded && move.magnitude > 0)
		{
			m_Animator.speed = m_AnimSpeedMultiplier;
		}
		else
		{
			// don't use that while airborne
			m_Animator.speed = 1;
		}
	}


	void HandleAirborneMovement()
	{
		// apply extra gravity from multiplier:
		Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
		m_Rigidbody.AddForce(extraGravityForce);

		m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
	}


	void HandleGroundedMovement(bool jump)
	{
		// check whether conditions are right to allow a jump:
		if (jump && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
		{
			// jump!
			m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
			m_IsGrounded = false;
			m_Animator.applyRootMotion = false;
			m_GroundCheckDistance = 0.1f;
		}
	}


	public void OnAnimatorMove()
	{
      // This callback from the animator theoretically gets called
      // for both local animation and replicated animation. So make
      // sure we're only updating the move for this local player..
      // we implement this function to override the default root motion.
      // this allows us to modify the positional speed before it's applied.
      if (isLocalPlayer)
      {
         if (m_IsGrounded && Time.deltaTime > 0)
         {
            // Rotate
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, Mathf.Abs(m_ForwardAmount));
            transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);

            // Move
            m_AnimWalkSpeed = m_Animator.GetFloat("WalkSpeed");
            float forwardAmount = m_AnimWalkSpeed * (m_MoveSpeedMultiplier * Mathf.Sign(m_ForwardAmount));
            m_Rigidbody.velocity = transform.forward * forwardAmount;
         }
      }
	}


	void CheckGroundStatus()
	{
		RaycastHit hitInfo;
#if UNITY_EDITOR
		// helper to visualise the ground check ray in the scene view
		Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
#endif
		// 0.1f is a small offset to start the ray from inside the character
		// it is also good to note that the transform position in the sample assets is at the base of the character
		if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
		{
			m_GroundNormal = hitInfo.normal;
			m_IsGrounded = true;
			m_Animator.applyRootMotion = true;
		}
		else
		{
			m_IsGrounded = false;
			m_GroundNormal = Vector3.up;
			m_Animator.applyRootMotion = false;
		}
	}

   /// <summary>
   /// OnFootstep
   /// Attenuate the footstep volume, and
   /// play the footstep sound
   /// </summary>
   public void OnFootstep()
   {
      // The Footstep animation pretty much always fires this event, even when we're blended to 
      // mostly idle, so only fire this  event if we're actually moving.
      // NOTE - Always get the speed from the animator component, not the local m_ForwardSpeed, as that value
      // won't be set on network clients.
      float speed = m_Animator.GetFloat("Forward");
      if (Mathf.Abs(speed) > 0.02f)
      {
         m_FootstepSound.Play();
      }
   }

   void HandleTurretRotation(float desiredTurretYaw, float desiredTurretPitch)
   {
      // If we have not defined a TurretObj, don't bother..
      if (!EnableTurretControl || TurretObj == null)
         return;

      

      // we make initial calculations from the original local rotation
      TurretObj.transform.localRotation = m_TurretOriginalRotation;

      //m_TurretYaw = Mathf.Lerp(m_TurretYaw, desiredTurretYaw, 1.1f * Time.deltaTime);
      //m_TurretPitch = Mathf.Lerp(m_TurretPitch, desiredTurretPitch, 1.1f * Time.deltaTime);
      m_TurretYaw = desiredTurretYaw;
      m_TurretPitch = desiredTurretPitch;

      // with mouse input, we have direct control with no springback required.
      float dampTime = 0.1f;
      if (Mathf.Abs(m_TurretPitch) < 0.1f && Mathf.Abs(m_TurretYaw) < 0.1f && AutoCentering)
      {
         m_TurretTargetAngles.y = -m_TurretFollowAngles.y;
         m_TurretTargetAngles.x = -m_TurretFollowAngles.x;
         dampTime = TurretRecenteringTime;
      }
      else
      {
         m_TurretTargetAngles.y += m_TurretYaw * TurretRotationSpeed;
         m_TurretTargetAngles.x += m_TurretPitch * TurretRotationSpeed;

         // clamp values to allowed range
         m_TurretTargetAngles.y = Mathf.Clamp(m_TurretTargetAngles.y, -TurretRotationRange.y * 0.5f, TurretRotationRange.y * 0.5f);
         m_TurretTargetAngles.x = Mathf.Clamp(m_TurretTargetAngles.x, -TurretRotationRange.x * 0.5f, TurretRotationRange.x * 0.5f);
         dampTime = TurretDampingTime;
      }

      // wrap values to avoid springing quickly the wrong way from positive to negative
      if (m_TurretTargetAngles.y > 180)
      {
         m_TurretTargetAngles.y -= 360;
         m_TurretFollowAngles.y -= 360;
      }
      if (m_TurretTargetAngles.x > 180)
      {
         m_TurretTargetAngles.x -= 360;
         m_TurretFollowAngles.x -= 360;
      }
      if (m_TurretTargetAngles.y < -180)
      {
         m_TurretTargetAngles.y += 360;
         m_TurretFollowAngles.y += 360;
      }
      if (m_TurretTargetAngles.x < -180)
      {
         m_TurretTargetAngles.x += 360;
         m_TurretFollowAngles.x += 360;
      }

      // smoothly interpolate current values to target angles
      m_TurretFollowAngles = Vector3.SmoothDamp(m_TurretFollowAngles, m_TurretTargetAngles, ref m_TurretFollowVelocity, dampTime);

      // update the actual gameobject's rotation
      TurretObj.transform.localRotation = m_TurretOriginalRotation * Quaternion.Euler(-m_TurretFollowAngles.x, m_TurretFollowAngles.y, 0);

   }


}
