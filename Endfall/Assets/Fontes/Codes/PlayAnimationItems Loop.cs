using UnityEngine;
using UnityEngine.UI;

public class AnimationTrigger : MonoBehaviour
{
    public Button triggerButton;
    public DragonBones.UnityArmatureComponent armatureUtama;
    public string animationName;

    void Start()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(PlayAnimation);
        }
    }

    void PlayAnimation()
    {
        var animationState = armatureUtama.animation.FadeIn(animationName, 0.2f);
        if (animationState != null)
        {
            animationState.Play();
        }
    }
}
