using UnityEngine;
using UnityEngine.UI;
using DragonBones;

public class ChangeCapeSkinFromAnotherArmature : MonoBehaviour
{
    public UnityArmatureComponent armatureUtama;
    public UnityArmatureComponent armatureSkin;

    public string capeSlotName = "CAPE";
    public string skinSlotName = "CAPE";

    public Button changeSkinButton;

    void Start()
    {
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.AddListener(ReplaceCapeSkin);
        }
    }

    void ReplaceCapeSkin()
    {
        Slot capeSlot = armatureUtama.armature.GetSlot(capeSlotName);

        Slot skinSlot = armatureSkin.armature.GetSlot(skinSlotName);

        if (capeSlot != null && skinSlot != null)
        {
            var newDisplay = skinSlot.display;

            capeSlot.display = newDisplay;

            capeSlot.offset.x = skinSlot.offset.x;
            capeSlot.offset.y = skinSlot.offset.y;
            capeSlot.offset.scaleX = skinSlot.offset.scaleX;
            capeSlot.offset.scaleY = skinSlot.offset.scaleY;
            capeSlot.offset.rotation = skinSlot.offset.rotation;

            armatureUtama.armature.AdvanceTime(0);
        }
        else
        {
            Debug.LogError("Slot not found in one or both armatures.");
        }
    }


}
