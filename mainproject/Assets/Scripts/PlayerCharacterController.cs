using UnityEngine;

public class PlayerCharacterController : MonoBehaviour
{
    #region variables/parameters
    // references
    private CharacterController characterController;
    private TwoDimensionalAnimationStateController animationController;
    private Transform cameraTransform;

    // movement parameters
    public float walkSpeed = 2.0f; 
    public float runSpeed = 4.0f;
    public float rotationSpeed = 10.0f;
   
    public float groundCheckDistance = 0.01f;
    
    // jump parameters
    public float jumpForce = 5.0f;
    public float gravity = 17.0f;
    [Header("Jump Curve Settings")] 
    public AnimationCurve jumpCurve;
    public float jumpDuration = 0.5f; // how long the curve evaluation lasts
    private float jumpTimer = 0f;
    private bool isJumping = false;
    
    [Header("Jump Tilt Settings")]
    public float jumpForwardTiltAngle = 10f; // forward tilt when taking off
    public float jumpBackwardTiltAngle = -50f; // backward tilt when falling
    public float tiltSpeed = 0.6f; // how quickly the character tilts
    public float minHorizontalSpeedForTilt = 3f; // minimum horizontal speed required for tilt
  
    [Header("Landing Animation Settings")]
    public float landingDetectionDistance = 1.0f;
    [Range(0.1f, 3.0f)]
    public float landingAnimationStartHeight = 1.0f;
    
    public LayerMask groundLayer;
    
    // movement state
    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded = false;
    private bool wasGrounded = false;
    private bool isLandingDetected = false;
    private float verticalVelocity = 0.0f;
    private float horizontalSpeed = 0f; // track horizontal speed for tilt condition
    private bool isAscending = false; // track if character is going up
    #endregion
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animationController = GetComponent<TwoDimensionalAnimationStateController>();
        
        cameraTransform = Camera.main.transform;
        
        if (animationController == null)
        {
            Debug.LogError("Animation controller component not found!");
        }
    }

    void Update()
    {
        wasGrounded = isGrounded;
        GroundCheck();
        CheckForLanding();
        HandleMovementInput();
        HandleJumpInput();
        ApplyGravity();
        
        isAscending = verticalVelocity > 0;
        
        ApplyJumpTilt();
        MoveCharacter();
        
        // handle landing states
        if (!wasGrounded && isGrounded)
        {
            animationController.OnJumpAnimationEnd();
            isLandingDetected = false;
            isJumping = false;
        }
    }
    
    void GroundCheck()
    {
        // using both raycast and character controllers isGrounded property
        RaycastHit hit;
        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hit, groundCheckDistance + 0.1f, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = characterController.isGrounded;
        }
    }
    
    void CheckForLanding()
    {
        // only check for landing if falling and haven't already detected landing
        if (!isGrounded && verticalVelocity < 0 && !isLandingDetected)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, landingAnimationStartHeight, groundLayer))
            {
                // approaching the ground -> start landing anim
                animationController.StartLanding();
                isLandingDetected = true;
            }
        }
    }
    
    void HandleMovementInput()
    {
        // keyboard input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // calculate move direction relative to camera orientation
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        Vector3 desiredMoveDirection = forward * verticalInput + right * horizontalInput;
        
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // set horizontal movement (keeping vertical velocity)
        if (desiredMoveDirection.magnitude > 0.1f)
        {
            // rotate character to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
            
            if (!isGrounded)
            {
                float currentXRotation = transform.rotation.eulerAngles.x;
                Vector3 targetEuler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(currentXRotation, targetEuler.y, 0);
            }
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // set move direction with current speed
            moveDirection.x = desiredMoveDirection.x * currentSpeed;
            moveDirection.z = desiredMoveDirection.z * currentSpeed;
        }
        else
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
        }
        horizontalSpeed = new Vector2(moveDirection.x, moveDirection.z).magnitude;
    }
    
    void HandleJumpInput()
    {
        // start jump when grounded and space is pressed
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
            jumpTimer = 0f;
            isLandingDetected = false;
            
            // apply initial jump velocity
            verticalVelocity = jumpForce * jumpCurve.Evaluate(0f);
            
            animationController.StartJump();
        }
        
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            
            if (jumpTimer <= jumpDuration)
            {
                float normalizedTime = jumpTimer / jumpDuration;
                
                verticalVelocity = jumpForce * jumpCurve.Evaluate(normalizedTime);
            }
            else
            {
                isJumping = false;
            }
        }
    }
    
    void ApplyGravity()
    {
        if (!isGrounded && !isJumping)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (isGrounded && !isJumping)
        {
            verticalVelocity = -0.5f;
        }
        
        moveDirection.y = verticalVelocity;
    }
    
    void ApplyJumpTilt()
    {
        // only apply tilt if moving horizontally @ set speed
        bool shouldTilt = !isGrounded && horizontalSpeed >= minHorizontalSpeedForTilt;
        
        float targetTilt = 0f;
        
        if (shouldTilt)
        {
            if (isAscending)
            {
                // forward tilt 
                targetTilt = jumpForwardTiltAngle;
            }
            else
            {
                // backward tilt
                targetTilt = jumpBackwardTiltAngle;
            }
        }
        
        // get current rotation
        Vector3 currentRotation = transform.rotation.eulerAngles;
        
        float currentXAngle = currentRotation.x;
        if (currentXAngle > 180)
            currentXAngle -= 360;
            
        float newXRotation = Mathf.LerpAngle(currentXAngle, targetTilt, Time.deltaTime * tiltSpeed);
        
        // apply the rotation
        transform.rotation = Quaternion.Euler(newXRotation, currentRotation.y, 0f);
    }
    
    void MoveCharacter()
    {
        characterController.Move(moveDirection * Time.deltaTime);
    }
}