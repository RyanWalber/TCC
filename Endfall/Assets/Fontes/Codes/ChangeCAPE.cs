using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonBones;

public class ChangeSkinSprite : MonoBehaviour
{
    public UnityArmatureComponent armatureComponent;

    public string slotName = "CAPE";

    public Sprite hitamSprite;
    public Material hitamMaterial;

    public Button changeSkinButton;

    void Start()
    {
        changeSkinButton.onClick.AddListener(OnChangeSkinButtonClicked);
    }

    void OnChangeSkinButtonClicked()
    {
        Slot slot = armatureComponent.armature.GetSlot(slotName);

        if (slot != null)
        {
            ReplaceSlotDisplay(slot);
        }
        else
        {
            Debug.LogError("Slot not found: " + slotName);
        }
    }

    void ReplaceSlotDisplay(Slot slot)
    {
        var spriteRenderer = slot.display as SpriteRenderer;
        if (spriteRenderer != null && hitamSprite != null)
        {
            spriteRenderer.sprite = hitamSprite;
            return;
        }

        var meshRenderer = slot.display as MeshRenderer;
        if (meshRenderer != null && hitamMaterial != null)
        {
            meshRenderer.sharedMaterial = hitamMaterial;
            return;
        }

        var displayObject = slot.display as GameObject;
        if (displayObject != null)
        {
            var renderer = displayObject.GetComponent<Renderer>();
            if (renderer != null && hitamMaterial != null)
            {
                renderer.sharedMaterial = hitamMaterial;
                return;
            }

            var image = displayObject.GetComponent<UnityEngine.UI.Image>();
            if (image != null && hitamSprite != null)
            {
                image.sprite = hitamSprite;
                return;
            }
        }

        Debug.LogError("Slot display is not a supported type.");
    }
}
