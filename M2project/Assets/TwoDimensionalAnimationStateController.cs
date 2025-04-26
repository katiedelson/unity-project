using System;
using UnityEngine;

public class TwoDimensionalAnimationStateController : MonoBehaviour
{
    private Animator animator;

    private float velocityZ = 0.0f;
    private float velocityX = 0.0f;
    private float jumpProgress = 0.0f;

    public float acceleration = 2.0f;
    public float deceleration = 2.0f;
    public float maximumWalkVelocity = 0.5f;
    public float maximumRunVelocity = 2.0f;

    // jump blend settings
    public float jumpBlendDuration = 3.0f; // higher val = faster pose transistions
    //public AnimationCurve jumpCurve;    // for progression

    // increase performance
    private int VelocityZHash;
    private int VelocityXHash;
    private int IsJumpingHash;
    private int JumpProgressHash;

    // state tracking
    private bool isJumping = false;
    private float jumpTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    // handles acceration and deceleration
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

        // increase velocityX if left isn ot pressed and velocity X < 0
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
        if (isJumping)
        {
            // Increase jump progress over time
            jumpProgress += Time.deltaTime / jumpBlendDuration;

            // Clamp between 0 and 1
            jumpProgress = Mathf.Clamp01(jumpProgress);

            // Update animator parameter to control the blend tree
            animator.SetFloat(JumpProgressHash, jumpProgress);
        }
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

        if (jumpPressed && !isJumping)
        {
            Debug.Log("jump triggered");
            isJumping = true;
            jumpProgress = 0f;
            animator.SetBool(IsJumpingHash, true);
        }

        UpdateJumpBlendTree();


        // set the parameters to our local variable values
        animator.SetFloat(VelocityZHash, velocityZ);
        animator.SetFloat(VelocityXHash, velocityX);
    }

    public void OnJumpAnimationEnd()
    {
        isJumping = false;
        jumpProgress = 0f;
        animator.SetBool(IsJumpingHash, false);
        animator.SetFloat(JumpProgressHash, 0f);
    }
}