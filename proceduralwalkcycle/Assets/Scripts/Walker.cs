using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walker : MonoBehaviour
{
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public AnimationCurve horizontalCurve;
    public AnimationCurve verticalCurve;
    public AnimationCurve jumpCurve; // Animation curve for jump height
    public float jumpHeight = 2f; // Maximum jump height
    public float jumpDuration = 1f; // How long the jump lasts
    public KeyCode jumpKey = KeyCode.Space; // Key to trigger jump
    public KeyCode walkKey = KeyCode.W; // Key to trigger walking
    public float walkSpeed = 1f; // Multiplier for walk speed
    public float jumpForwardSpeed = 2f;
    
    private Vector3 leftTargetOffset;
    private Vector3 rightTargetOffset;
    private float leftLegLast = 0;
    private float rightLegLast = 0;
    
    private bool isJumping = false;
    private float jumpStartTime = 0f;
    private Vector3 jumpStartPosition;
    private Vector3 leftFootJumpPos;
    private Vector3 rightFootJumpPos;
    private bool wasWalkingWhenJumped = false;
    
    // Animation time tracking
    private float animationTime = 0f;
    private bool isWalking = false;
    
    // Start is called before the first frame update
    void Start()
    {
        leftTargetOffset = leftFootTarget.localPosition;
        rightTargetOffset = rightFootTarget.localPosition;
        
        // If no jump curve is assigned, create a default one
        if (jumpCurve == null || jumpCurve.keys.Length == 0)
        {
            jumpCurve = new AnimationCurve(
                new Keyframe(0, 0, 2, 2),
                new Keyframe(0.5f, 1, 0, 0),
                new Keyframe(1, 0, -2, -2)
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if walking key is pressed
        isWalking = Input.GetKey(walkKey);
        
        // Only update animation time when walking or jumping
        if (isWalking || isJumping)
        {
            animationTime += Time.deltaTime * walkSpeed;
        }
        
        // Check for jump input - allow jump anytime, whether walking or not
        if (Input.GetKeyDown(jumpKey) && !isJumping)
        {
            StartJump();
        }

        if (isJumping)
        {
            UpdateJump();
        }
        else
        {
            UpdateWalk();
        }
    }

    void StartJump()
    {
        isJumping = true;
        jumpStartTime = Time.time;
        jumpStartPosition = transform.position;
        
        // Store the current foot positions to use during jump
        leftFootJumpPos = leftFootTarget.localPosition;
        rightFootJumpPos = rightFootTarget.localPosition;
        
        // Remember if we were walking when jump started
        wasWalkingWhenJumped = isWalking;
    }

    void UpdateJump()
    {
        float jumpProgress = (Time.time - jumpStartTime) / jumpDuration;
        
        if (jumpProgress >= 1.0f)
        {
            // Jump is complete
            isJumping = false;
            
            // Make sure we're grounded after landing
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
            return;
        }

        // Apply jump height based on animation curve
        float jumpFactor = jumpCurve.Evaluate(jumpProgress);
        Vector3 currentPos = transform.position;
        currentPos.y = jumpStartPosition.y + jumpHeight * jumpFactor;
        transform.position = currentPos;
        
        // Move the character forward during jump ONLY if W is pressed
        // Either it was pressed when jump started or it's pressed now
        if (isWalking)
        {
            transform.position += transform.forward * Time.deltaTime * walkSpeed * jumpForwardSpeed;
        }
        
        // During jump, animate feet to look natural
        float legTuckFactor = Mathf.Sin(jumpProgress * Mathf.PI) * 0.5f;
        
        Vector3 jumpOffset = transform.InverseTransformVector(transform.up) * legTuckFactor;
        
        leftFootTarget.localPosition = Vector3.Lerp(
            leftFootJumpPos,
            leftTargetOffset + jumpOffset,
            legTuckFactor * 2
        );
        
        rightFootTarget.localPosition = Vector3.Lerp(
            rightFootJumpPos,
            rightTargetOffset + jumpOffset,
            legTuckFactor * 2
        );
    }

    void UpdateWalk()
    {
        // Use our controlled animation time instead of Time.time for consistent animations
        float leftLegForwardMovement = horizontalCurve.Evaluate(animationTime);
        float rightLegForwardMovement = horizontalCurve.Evaluate(animationTime - 1);

        leftFootTarget.localPosition = leftTargetOffset + 
            this.transform.InverseTransformVector(leftFootTarget.forward) * leftLegForwardMovement + 
            this.transform.InverseTransformVector(leftFootTarget.up) * verticalCurve.Evaluate(animationTime + 0.5f);
        
        rightFootTarget.localPosition = rightTargetOffset + 
            this.transform.InverseTransformVector(rightFootTarget.forward) * rightLegForwardMovement + 
            this.transform.InverseTransformVector(leftFootTarget.up) * verticalCurve.Evaluate(animationTime - 0.5f);

        float leftLegDirection = leftLegForwardMovement - leftLegLast;
        float rightLegDirection = rightLegForwardMovement - rightLegLast;

        // Only move forward if walking key is pressed
        if (isWalking)
        {
            RaycastHit hit;
            if (leftLegDirection < 0 && Physics.Raycast(leftFootTarget.position + leftFootTarget.up, -leftFootTarget.up, out hit, Mathf.Infinity))
            {
                leftFootTarget.position = hit.point;
                this.transform.position += this.transform.forward * Mathf.Abs(leftLegDirection);
            }

            if (rightLegDirection < 0 && Physics.Raycast(rightFootTarget.position + rightFootTarget.up, -rightFootTarget.up, out hit, Mathf.Infinity))
            {
                rightFootTarget.position = hit.point;
                this.transform.position += this.transform.forward * Mathf.Abs(rightLegDirection);
            }
        }
        else
        {
            // When not walking, keep feet in a neutral idle position
            // But still position on ground
            RaycastHit hit;
            
            // Place left foot at a slightly offset idle position
            Vector3 leftIdlePos = leftTargetOffset;
            if (Physics.Raycast(transform.position + transform.TransformVector(leftIdlePos) + Vector3.up, Vector3.down, out hit, Mathf.Infinity))
            {
                leftFootTarget.position = hit.point;
            }
            
            // Place right foot at a slightly offset idle position
            Vector3 rightIdlePos = rightTargetOffset;
            if (Physics.Raycast(transform.position + transform.TransformVector(rightIdlePos) + Vector3.up, Vector3.down, out hit, Mathf.Infinity))
            {
                rightFootTarget.position = hit.point;
            }
        }

        leftLegLast = leftLegForwardMovement;
        rightLegLast = rightLegForwardMovement;
    }
}