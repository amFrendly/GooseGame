using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    [SerializeField]
    CharacterController controller;

    [SerializeField]
    Transform cam;

    [SerializeField]
    float speed, jumpSpeed, gravity;

    [SerializeField]
    float turnSmoothTime;

    float turnSmoothVelocity;
    float ySpeed;

    // Update is called once per frame
    void Update()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool jump = Input.GetKey(KeyCode.Space);

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 moveDir = Vector3.zero;

        if (direction.magnitude > 0)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0, angle, 0);
            moveDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }

        Vector3 velocity = moveDir.normalized * speed;

        ySpeed -= gravity * Time.deltaTime;

        if (controller.isGrounded)
        {
            ySpeed = -0.5f;

            if (jump)
                ySpeed = jumpSpeed;
        }

        velocity.y = ySpeed;

        controller.Move(velocity * Time.deltaTime);
    }
}
