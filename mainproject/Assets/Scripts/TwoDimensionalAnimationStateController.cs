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
    public float acceleration = 3.0f;
    public float deceleration = 4.0f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;

    [Header("Jump Blend Settings")]
    public float jumpBlendDuration = 0.5f;
    public float landingBlendDuration = 1.2f; 
    public float crouchPosition = 0.5f;
    
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
        animator = GetComponent<Animator>();
        
        VelocityZHash = Animator.StringToHash("VelocityZ");
        VelocityXHash = Animator.StringToHash("VelocityX");
        IsJumpingHash = Animator.StringToHash("isJumping");
        JumpProgressHash = Animator.StringToHash("JumpProgress");
    }
    
    // acceleration and deceleration
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
    
        // left movement - decelerate when over max velocity
        if (leftPressed && velocityX < -currentMaxVelocity)
        {
            velocityX += Time.deltaTime * deceleration;
            // round to the -currentMaxVelocity if within offset
            if (velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity - 0.05f))
            {
                velocityX = -currentMaxVelocity;
            }
        }
        // round to the -currentMaxVelocity if within offset
        else if (leftPressed && velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity + 0.05f))
        {
            velocityX = -currentMaxVelocity;
        }
    
        // right movement - decelerate when over max velocity
        if (rightPressed && velocityX > currentMaxVelocity)
        {
            velocityX -= Time.deltaTime * deceleration;
            // round to the currentMaxVelocity if within offset
            if (velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + 0.05f))
            {
                velocityX = currentMaxVelocity;
            }
        }
        // round to the currentMaxVelocity if within offset
        else if (rightPressed && velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - 0.05f))
        {
            velocityX = currentMaxVelocity;
        }
    }

    void UpdateJumpBlendTree()
    {
        if (!isJumping)
            return;
            
        switch (currentJumpPhase)
        {
            case JumpPhase.Takeoff:
                // increase jump var
                jumpProgress += Time.deltaTime / jumpBlendDuration;
                
                // move to mid-air pose
                if (jumpProgress >= 1.0f)
                {
                    jumpProgress = 1.0f;
                    currentJumpPhase = JumpPhase.MidAir;
                }
                break;
                
            case JumpPhase.MidAir:
                jumpProgress = 1.0f;
                break;
                
            case JumpPhase.Landing:
                // move back to crouch
                jumpProgress -= Time.deltaTime / landingBlendDuration;
                
                // clamp to crouch position
                if (jumpProgress <= crouchPosition)
                {
                    jumpProgress = crouchPosition;
                }
                break;
        }
        animator.SetFloat(JumpProgressHash, jumpProgress);
    }
    
    void Update()
    {
        // key input
        bool forwardPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        bool leftPressed = Input.GetKey(KeyCode.A);
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool runPressed = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        
        float currentMaxVelocity = runPressed ? maximumRunVelocity : maximumWalkVelocity;
        
        changeVelocity(forwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);
        lockOrResetVelocity(forwardPressed, leftPressed, rightPressed, runPressed, currentMaxVelocity);

        // backup
        if (jumpPressed && !isJumping)
        {
            StartJump();
        }
        UpdateJumpBlendTree();
        
        
        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
    }

    public void StartJump()
    {
        if (!isJumping)
        {
            isJumping = true;
            jumpProgress = 0f;
            currentJumpPhase = JumpPhase.Takeoff;
            
            animator.SetBool(IsJumpingHash, true);
        }
    }
    
    // when approaching ground
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
        
        animator.SetBool(IsJumpingHash, false);
        animator.SetFloat(JumpProgressHash, 0f);
    }
}