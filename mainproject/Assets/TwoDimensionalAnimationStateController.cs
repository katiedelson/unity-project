using System;
using UnityEngine;

public class TwoDimensionalAnimationStateController : MonoBehaviour
{
    #region variables/parameters
    private Animator animator;

    private float velocityZ = 0.0f;
    private float velocityX = 0.0f;
    private float jumpProgress = 0.0f;

    [Header("Movement Settings")]
    public float acceleration = 1.0f;
    public float deceleration = 3.0f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;

    [Header("Jump Blend Settings")]
    public float jumpBlendDuration = 0.5f; // duration for going from idle to full jump
    public float landingBlendDuration = 1.2f; // duration for landing animation
    public float crouchPosition = 0.4f; // customizable crouch position in blend tree
    
    // increase performance
    private int VelocityZHash;
    private int VelocityXHash;
    private int IsJumpingHash;
    private int JumpProgressHash;
    
    // state tracking
    private bool isJumping = false;
    
    // jumping phases
    public enum JumpPhase { None, Takeoff, MidAir, Landing }
    private JumpPhase currentJumpPhase = JumpPhase.None;
    
    #endregion
    
    void Start()
    {
        // search the gameobject this script is attached to and get the animator component
        animator = GetComponent<Animator>();
        
        // increase performance
        VelocityZHash = Animator.StringToHash("VelocityZ");
        VelocityXHash = Animator.StringToHash("VelocityX");
        IsJumpingHash = Animator.StringToHash("isJumping");
        JumpProgressHash = Animator.StringToHash("JumpProgress");
    }
    
    // handles acceleration and deceleration
    void changeVelocity(bool forwardPressed, bool leftPressed, bool rightPressed, bool runPressed,
        float currentMaxVelocity)
    {
        // if player pressed forward, increase velocity in z direction
        if (forwardPressed && velocityZ < currentMaxVelocity)
        {
            velocityZ += Time.deltaTime * acceleration;
        }

        if (leftPressed && velocityX > -currentMaxVelocity)
        {
            velocityX -= Time.deltaTime * acceleration;
        }

        if (rightPressed && velocityX < currentMaxVelocity)
        {
            velocityX += Time.deltaTime * acceleration;
        }
        
        // decrease velocity
        if (!forwardPressed && velocityZ > 0.0f)
        {
            velocityZ -= Time.deltaTime * deceleration;
        }
        
        // increase velocityX if left isn't pressed and velocity X < 0
        if (!leftPressed && velocityX < 0.0f)
        {
            velocityX += Time.deltaTime * deceleration;
        }
        // decrease velocityX if right is not pressed and velocityX > 0
        if (!rightPressed & velocityX > 0.0f)
        {
            velocityX -= Time.deltaTime * deceleration;
        }
    }
    
    // handles reset and locking of velocity
    void lockOrResetVelocity(bool forwardPressed, bool leftPressed, bool rightPressed, bool runPressed,
        float currentMaxVelocity)
    {
        // reset velocity Z
        if (!forwardPressed && velocityZ < 0.0f)
        {
            velocityZ = 0.0f;
        }
        
        // reset velocityX
        if (!leftPressed && !rightPressed && velocityX != 0.0f && (velocityX > -0.05f && velocityX < 0.05f))
        {
            velocityX = 0.0f;
        }
        
        // lock forward
        if (forwardPressed && runPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ = currentMaxVelocity;
        }
        // decelerate to the maximum walk velocity
        else if (forwardPressed && velocityZ > currentMaxVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;
            // round to the currentMaxVelocity if within offset
            if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + 0.05f))
            {
                velocityZ = currentMaxVelocity;
            }
        }
        // round to the currentMaxVelocity if within offset
        else if (forwardPressed && velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - 0.05f))
        {
            velocityZ = currentMaxVelocity;
        }
    }

    void UpdateJumpBlendTree()
    {
        if (!isJumping)
            return;
            
        switch (currentJumpPhase)
        {
            case JumpPhase.Takeoff:
                // increase jump progress during takeoff phase
                jumpProgress += Time.deltaTime / jumpBlendDuration;
                
                // move to mid-air pose
                if (jumpProgress >= 1.0f)
                {
                    jumpProgress = 1.0f;
                    currentJumpPhase = JumpPhase.MidAir;
                }
                break;
                
            case JumpPhase.MidAir:
                // stay at mid-air pose
                jumpProgress = 1.0f;
                break;
                
            case JumpPhase.Landing:
                // move from mid-air pose back to crouch pose
                jumpProgress -= Time.deltaTime / landingBlendDuration;
                
                // clamp to crouch position
                if (jumpProgress <= crouchPosition)
                {
                    jumpProgress = crouchPosition;
                }
                break;
        }
        
        // Update animator parameter to control the blend tree
        animator.SetFloat(JumpProgressHash, jumpProgress);
    }

    // update is called once per frame
    void Update()
    {
        // input will be true if player is pressing on the passed in key parameter
        // get key input from player
        bool forwardPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        bool leftPressed = Input.GetKey(KeyCode.A);
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool runPressed = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        
        // set current maxVelocity
        float currentMaxVelocity = runPressed ? maximumRunVelocity : maximumWalkVelocity;
        
        // handle changes in velocity
        changeVelocity(forwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);
        lockOrResetVelocity(forwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);

        // check for jump input directly in controller
        // backup to ensure the animation state gets updated even if the character controller misses it
        if (jumpPressed && !isJumping)
        {
            StartJump();
        }

        UpdateJumpBlendTree();
        
        // set the parameters to our local variable values
        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
    }

    // called from character controller
    public void StartJump()
    {
        if (!isJumping)
        {
            isJumping = true;
            jumpProgress = 0f;
            currentJumpPhase = JumpPhase.Takeoff;
            
            // update animators isJumping parameter to start transition
            animator.SetBool(IsJumpingHash, true);
        }
    }
    
    // called from character controller when approaching ground
    public void StartLanding()
    {
        if (isJumping && currentJumpPhase != JumpPhase.Landing)
        {
            currentJumpPhase = JumpPhase.Landing;
        }
    }

    public void OnJumpAnimationEnd()
    {
        isJumping = false;
        currentJumpPhase = JumpPhase.None;
        jumpProgress = 0f;
        
        // update the animator's isJumping parameter to exit jump state!!
        animator.SetBool(IsJumpingHash, false);
        animator.SetFloat(JumpProgressHash, 0f);
    }
}