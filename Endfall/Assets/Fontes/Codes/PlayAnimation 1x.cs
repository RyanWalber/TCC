using UnityEngine;
using UnityEngine.UI;

public class AnimationOneTime : MonoBehaviour
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
        var animationState = armatureUtama.animation.FadeIn(animationName, 0.2f, 1);
        if (animationState != null)
        {
            animationState.Play();
        }
    }
}
