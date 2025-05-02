using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private Vector3 lastPosition;

    [Header("Optional Head Look Settings")]
    public Transform headBone; // robot's head bone
    public Transform cameraAttachPoint;
    public float headRotationSpeed = 5f;
    public float maxHeadTurnAngle = 70f;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // movement direction
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;
        delta.y = 0; // ignore vertical movement

        Vector3 localDelta = transform.InverseTransformDirection(delta); // global -> local movement (relative to player)
        float moveX = localDelta.x / Time.deltaTime;
        float moveZ = localDelta.z / Time.deltaTime;
        float moveSpeed = new Vector3(localDelta.x, 0, localDelta.z).magnitude / Time.deltaTime;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(moveZ) > 0.1f; // only allow forward/back
        
        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveZ", moveZ);
        animator.SetBool("IsRunning", isRunning);

        lastPosition = currentPosition;

        // head follow camera
        if (headBone != null && cameraAttachPoint != null)
        {
            Vector3 directionToLook = cameraAttachPoint.position + cameraAttachPoint.forward * 10f - headBone.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToLook);
            
            // clamp rotation angle
            float angle = Vector3.Angle(headBone.forward, directionToLook);
            if (angle < maxHeadTurnAngle)
                headBone.rotation = Quaternion.Slerp(headBone.rotation, targetRotation, headRotationSpeed * Time.deltaTime);
        }
    }
}