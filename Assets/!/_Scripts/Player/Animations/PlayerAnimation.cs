using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;  // on robot
    private Vector3 lastPosition;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;

        delta.y = 0; // ignore vertical movement

        // local-space movement
        Vector3 localDelta = transform.InverseTransformDirection(delta); // global -> local movement (relative to player)
        float moveX = localDelta.x / Time.deltaTime;
        float moveZ = localDelta.z / Time.deltaTime;

        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveZ", moveZ);

        lastPosition = currentPosition;
    }
}