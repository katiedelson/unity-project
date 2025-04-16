using UnityEngine;

public class PlayerCharacterController : MonoBehaviour
{
    // references
    private CharacterController characterController;
    private TwoDimensionalAnimationStateController animationController;
    private Transform cameraTransform;

    // movement parameters
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    
    // jump parameters
    public float jumpForce = 8.0f;
    public float gravity = 20.0f;
    
    // ground detection
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    
    // movement state
    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded = false;
    private float verticalVelocity = 0.0f;
    private bool wasGrounded = false;

    void Start()
    {
        // get component references
        characterController = GetComponent<CharacterController>();
        animationController = GetComponent<TwoDimensionalAnimationStateController>();
        
        // get main camera transform for movement direction
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        wasGrounded = isGrounded;
        GroundCheck();
        HandleMovementInput();
        HandleJumpInput();
        ApplyGravity();
        MoveCharacter();
        
        if (!wasGrounded && isGrounded)
        {
            // landed --> call the jump animation end function
            animationController.OnJumpAnimationEnd();
        }
    }
    
    void GroundCheck()
    {
        // Using both raycast and character controller's isGrounded property
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
    
    void HandleMovementInput()
    {
        // get input from keyboard
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // calculate move direction relative to camera orientation
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        // project vectors onto the horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // create movement vector
        Vector3 desiredMoveDirection = forward * verticalInput + right * horizontalInput;
        
        // set speed based on running state
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        // set horizontal movement (keeping vertical velocity)
        if (desiredMoveDirection.magnitude > 0.1f)
        {
            // rotate character to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // set move direction with current speed
            moveDirection.x = desiredMoveDirection.x * currentSpeed;
            moveDirection.z = desiredMoveDirection.z * currentSpeed;
        }
        else
        {
            // no input, stop horizontal movement
            moveDirection.x = 0;
            moveDirection.z = 0;
        }
    }
    
    void HandleJumpInput()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            // Apply jump force
            verticalVelocity = jumpForce;
            Debug.Log("Jump initiated with force: " + jumpForce);
        }
    }
    
    void ApplyGravity()
    {
        if (!isGrounded)
        {
            // Apply gravity when in air
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            // Reset vertical velocity when grounded
            verticalVelocity = -0.5f; // Small downward force to keep grounded
        }
        
        // Update the y component of moveDirection
        moveDirection.y = verticalVelocity;
    }
    
    void MoveCharacter()
    {
        // move the character using the character controller
        characterController.Move(moveDirection * Time.deltaTime);
    }
}