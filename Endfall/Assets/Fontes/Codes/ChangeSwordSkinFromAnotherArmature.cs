using UnityEngine;
using UnityEngine.UI;
using DragonBones;

public class ChangeSwordSkinFromAnotherArmature : MonoBehaviour
{
    public UnityArmatureComponent armatureUtama;
    public UnityArmatureComponent armatureSkin;

    public string swordSlotName = "SWORD";
    public string skinSlotName = "SWORD";

    public Button changeSkinButton;

    void Start()
    {
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.AddListener(ReplaceSwordSkin);
        }
    }

    void ReplaceSwordSkin()
    {
        Slot swordSlot = armatureUtama.armature.GetSlot(swordSlotName);

        Slot skinSlot = armatureSkin.armature.GetSlot(skinSlotName);

        if (swordSlot != null && skinSlot != null)
        {
            var newDisplay = skinSlot.display;

            swordSlot.display = newDisplay;

            swordSlot.offset.x = skinSlot.offset.x;
            swordSlot.offset.y = skinSlot.offset.y;
            swordSlot.offset.scaleX = skinSlot.offset.scaleX;
            swordSlot.offset.scaleY = skinSlot.offset.scaleY;
            swordSlot.offset.rotation = skinSlot.offset.rotation;

            armatureUtama.armature.AdvanceTime(0);
        }
        else
        {
            Debug.LogError("Slot not found in one or both armatures.");
        }
    }

    void OnDestroy()
    {
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.RemoveListener(ReplaceSwordSkin);
        }
    }
}
