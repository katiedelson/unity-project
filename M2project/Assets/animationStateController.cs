using UnityEngine;

public class animationStateController : MonoBehaviour
{
    private Animator animator;
    private float velocity = 0.0f;
    public float acceleration = 0.1f;
    public float deceleration = 0.5f;
    private int VelocityHash;    
    void Start()
    {
        // set reference for animator
        animator = GetComponent<Animator>();
        
        // increases performance
        VelocityHash = Animator.StringToHash("Velocity");
    }

    void Update()
    {
        // get key input from player
        bool forwardPressed = Input.GetKey("w");
        bool runPressed = Input.GetKey("left shift");

        if (forwardPressed && velocity < 1.0f)
        {
            velocity += Time.deltaTime * acceleration;
        }
        if (!forwardPressed && velocity > 0.0f)
        {
            velocity -= Time.deltaTime * deceleration;
        }

        if (!forwardPressed && velocity < 0.0f)
        {
            velocity = 0.0f;
        }
        
        animator.SetFloat(VelocityHash, velocity);
    }
}

/*
using UnityEngine;

public class animationStateController : MonoBehaviour
{
    private Animator animator;
    private int isWalkingHash;
    int isRunningHash;
    void Start()
    {
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
    }

    void Update()
    {
        bool isRunning = animator.GetBool(isRunningHash);
        bool isWalking = animator.GetBool(isWalkingHash);
        bool forwardPressed = Input.GetKey("w");
        bool runPressed = Input.GetKey("left shift");
        //if player presses w key
        if (!isWalking && forwardPressed)
        {
            // then set the isWalking boolean to be true
            animator.SetBool(isWalkingHash, true);
        }
        // if player isn't pressing w key
        if (isWalking && !forwardPressed)
        {
            // then set isWalking boolean to be false
            animator.SetBool(isWalkingHash, false);
        }
        
        // if player is walking, not running, and presses left shift
        if (!isRunning && (forwardPressed && runPressed))
        {
            animator.SetBool(isRunningHash, true);
        }
        // if player is running and stops running or stops walking
        if (isRunning && (!forwardPressed || !runPressed))
        {
            animator.SetBool(isRunningHash, false);
        }
    }
}
*/