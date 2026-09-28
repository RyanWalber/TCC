using UnityEngine;

public class BossAnimationEvents : MonoBehaviour
{
    private BossFomeController controller;

    private void Awake()
    {
        controller = GetComponentInParent<BossFomeController>();
    }

    public void DispararFlecha()
    {
        if (controller != null)
        {
            controller.DispararFlecha();
        }
    }
}