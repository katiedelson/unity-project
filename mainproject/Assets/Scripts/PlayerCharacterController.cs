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
    public float jumpDuration = 0.5f;
    private float jumpTimer = 0f;
    private bool isJumping = false;
    
    [Header("Jump Tilt Settings")]
    public float jumpForwardTiltAngle = 10f;
    public float jumpBackwardTiltAngle = -50f;
    public float tiltSpeed = 0.6f;
    public float minHorizontalSpeedForTilt = 3f;
  
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
    private float horizontalSpeed = 0f;
    private bool isAscending = false;
    #endregion
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animationController = GetComponent<TwoDimensionalAnimationStateController>();
        
        // get main camera transform for movement direction
        cameraTransform = Camera.main.transform;
        
        // fix itch.io cursor bug
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
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
        
        // track if ascending or descending
        isAscending = verticalVelocity > 0;
        
        ApplyJumpTilt(); 
        MoveCharacter();
        
        if (!wasGrounded && isGrounded)
        {
            animationController.OnJumpAnimationEnd();
            isLandingDetected = false;
            isJumping = false;
        }
        
        // cursor bug fix
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // lock cursor state until tab pressed
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    void GroundCheck()
    {
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
        // keyboard
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        // calculate move direction relative to camera orientation
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        Vector3 desiredMoveDirection = forward * verticalInput + right * horizontalInput;
        
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // horizontal movement
        if (desiredMoveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
            
            // only rotate around Y axis while in air
            if (!isGrounded)
            {
                float currentXRotation = transform.rotation.eulerAngles.x;
                Vector3 targetEuler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(currentXRotation, targetEuler.y, 0);
            }
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            moveDirection.x = desiredMoveDirection.x * currentSpeed;
            moveDirection.z = desiredMoveDirection.z * currentSpeed;
        }
        else
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
        }
        
        //for tilt
        horizontalSpeed = new Vector2(moveDirection.x, moveDirection.z).magnitude;
    }
    
    void HandleJumpInput()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
            jumpTimer = 0f;
            isLandingDetected = false;
            
            verticalVelocity = jumpForce * jumpCurve.Evaluate(0f);
            
            animationController.StartJump();
        }
        
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            
            if (jumpTimer <= jumpDuration)
            {
                //(0 to 1)
                float normalizedTime = jumpTimer / jumpDuration;
                
                // apply the curve
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
        // apply gravity when not in jumping
        if (!isGrounded && !isJumping)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (isGrounded && !isJumping)
        {
            // reset vertical velocity on ground
            verticalVelocity = -0.5f;
        }
        moveDirection.y = verticalVelocity;
    }
    
    void ApplyJumpTilt()
    {
        // only apply tilt if running
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
        
        // get rotation
        Vector3 currentRotation = transform.rotation.eulerAngles;
     
        float currentXAngle = currentRotation.x;
        if (currentXAngle > 180)
            currentXAngle -= 360;
 
        float newXRotation = Mathf.LerpAngle(currentXAngle, targetTilt, Time.deltaTime * tiltSpeed);
        
        // apply
        transform.rotation = Quaternion.Euler(newXRotation, currentRotation.y, 0f);
    }
    
    void MoveCharacter()
    {
        characterController.Move(moveDirection * Time.deltaTime);
    }
}