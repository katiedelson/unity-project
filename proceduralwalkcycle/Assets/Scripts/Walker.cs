using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walker : MonoBehaviour
{
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public AnimationCurve horizontalCurve;
    public AnimationCurve verticalCurve;
    public AnimationCurve jumpCurve;
    public float jumpHeight = 2f; 
    public float jumpDuration = 1f; 
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode walkKey = KeyCode.W;
    public float walkSpeed = 1f;
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
    
    // animation time tracking
    private float animationTime = 0f;
    private bool isWalking = false;
    
    void Start()
    {
        leftTargetOffset = leftFootTarget.localPosition;
        rightTargetOffset = rightFootTarget.localPosition;
        
        // if no jump curve is assigned, create a default one
        if (jumpCurve == null || jumpCurve.keys.Length == 0)
        {
            jumpCurve = new AnimationCurve(
                new Keyframe(0, 0, 2, 2),
                new Keyframe(0.5f, 1, 0, 0),
                new Keyframe(1, 0, -2, -2)
            );
        }
    }

    void Update()
    {
        isWalking = Input.GetKey(walkKey);
        
        // only update when walking or jumping
        if (isWalking || isJumping)
        {
            animationTime += Time.deltaTime * walkSpeed;
        }
        
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
        
        leftFootJumpPos = leftFootTarget.localPosition;
        rightFootJumpPos = rightFootTarget.localPosition;
        
        wasWalkingWhenJumped = isWalking;
    }

    void UpdateJump()
    {
        float jumpProgress = (Time.time - jumpStartTime) / jumpDuration;
        
        if (jumpProgress >= 1.0f)
        {
            isJumping = false;
            
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
            return;
        }

        // applying height based on curve
        float jumpFactor = jumpCurve.Evaluate(jumpProgress);
        Vector3 currentPos = transform.position;
        currentPos.y = jumpStartPosition.y + jumpHeight * jumpFactor;
        transform.position = currentPos;
        
        if (isWalking)
        {
            transform.position += transform.forward * Time.deltaTime * walkSpeed * jumpForwardSpeed;
        }
        
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
            RaycastHit hit;
            
            Vector3 leftIdlePos = leftTargetOffset;
            if (Physics.Raycast(transform.position + transform.TransformVector(leftIdlePos) + Vector3.up, Vector3.down, out hit, Mathf.Infinity))
            {
                leftFootTarget.position = hit.point;
            }
            
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