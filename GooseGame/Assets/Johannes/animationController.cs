using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationController : MonoBehaviour
{
    [SerializeField]
    CharacterController controller;

    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Velocity", new Vector2(controller.velocity.x, controller.velocity.z).magnitude);
        animator.SetFloat("Jump", controller.velocity.y);
    }
}
