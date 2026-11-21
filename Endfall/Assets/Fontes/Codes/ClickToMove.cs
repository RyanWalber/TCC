using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragonBones;

public class DragonBonesClickToMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private UnityArmatureComponent armatureComponent;
    private string currentAnimation;

    void Start()
    {
        armatureComponent = GetComponent<UnityArmatureComponent>();
        targetPosition = transform.position;
        currentAnimation = "SWORD_Idle";
        armatureComponent.animation.Play(currentAnimation);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                targetPosition = hit.point;
                isMoving = true;

                if (currentAnimation != "SWORD_Run_Weapon")
                {
                    currentAnimation = "SWORD_Run_Weapon";
                    armatureComponent.animation.Play(currentAnimation);
                }
            }
        }

        if (isMoving)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance > 0.1f)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;

                if (direction.x > 0)
                {
                    armatureComponent.armature.flipX = false;
                }
                else if (direction.x < 0)
                {
                    armatureComponent.armature.flipX = true;
                }
            }
            else
            {
                isMoving = false;
                if (currentAnimation != "SWORD_Idle")
                {
                    currentAnimation = "SWORD_Idle";
                    armatureComponent.animation.Play(currentAnimation);
                }
            }
        }
    }
}

