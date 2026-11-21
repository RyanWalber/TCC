using UnityEngine;
using UnityEngine.UI;
using DragonBones;

public class ReplaceAndReplaySlotDisplay : MonoBehaviour
{
    public UnityArmatureComponent armatureUtama;
    public UnityArmatureComponent armatureSkin;

    public string slotName = "SWORD";
    public string displayName = "Sword_Skin_01";
    public string animationName = "SWORD_Idle";

    public Button changeSkinButton;

    void Start()
    {
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.AddListener(ReplaceAndReplaySlot);
        }
    }

    void ReplaceAndReplaySlot()
    {
        if (armatureUtama != null && armatureSkin != null)
        {
            Slot targetSlot = armatureUtama.armature.GetSlot(slotName);
            Slot sourceSlot = armatureSkin.armature.GetSlot(slotName);

            if (targetSlot != null && sourceSlot != null)
            {
                var oldDisplay = targetSlot.display as GameObject;

                targetSlot.display = sourceSlot.display;

                if (targetSlot.display is GameObject newDisplay && oldDisplay != null)
                {
                    newDisplay.transform.localPosition = oldDisplay.transform.localPosition;
                    newDisplay.transform.localRotation = oldDisplay.transform.localRotation;
                    newDisplay.transform.localScale = oldDisplay.transform.localScale;
                }

                armatureUtama.armature.AdvanceTime(0);

                var animationState = armatureUtama.animation.FadeIn(animationName, 0.2f);
                if (animationState != null)
                {
                    animationState.Play();
                }
            }
            else
            {
                Debug.LogError("TargetSlot atau SourceSlot tidak ditemukan di armature.");
            }
        }
        else
        {
            Debug.LogError("ArmatureUtama atau ArmatureSkin tidak diatur dengan benar.");
        }
    }

    void OnDestroy()
    {
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.RemoveListener(ReplaceAndReplaySlot);
        }
    }
}
