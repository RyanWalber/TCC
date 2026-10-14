using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Tambahkan namespace ini untuk menggunakan UI components
using DragonBones;

public class ChangeSkinSprite : MonoBehaviour
{
    // Referensi ke armature component
    public UnityArmatureComponent armatureComponent;

    // Nama slot yang ingin diubah
    public string slotName = "CAPE";

    // Sprite baru atau material baru yang akan digunakan
    public Sprite hitamSprite;
    public Material hitamMaterial;

    // Referensi ke button UI
    public Button changeSkinButton;

    void Start()
    {
        // Menambahkan listener untuk mendeteksi klik pada tombol
        changeSkinButton.onClick.AddListener(OnChangeSkinButtonClicked);
    }

    void OnChangeSkinButtonClicked()
    {
        // Mendapatkan slot dari armature
        Slot slot = armatureComponent.armature.GetSlot(slotName);

        if (slot != null)
        {
            // Coba ganti display berdasarkan tipe display yang digunakan
            ReplaceSlotDisplay(slot);
        }
        else
        {
            Debug.LogError("Slot not found: " + slotName);
        }
    }

    void ReplaceSlotDisplay(Slot slot)
    {
        // Coba sebagai SpriteRenderer
        var spriteRenderer = slot.display as SpriteRenderer;
        if (spriteRenderer != null && hitamSprite != null)
        {
            spriteRenderer.sprite = hitamSprite;
            return;
        }

        // Coba sebagai MeshRenderer
        var meshRenderer = slot.display as MeshRenderer;
        if (meshRenderer != null && hitamMaterial != null)
        {
            meshRenderer.sharedMaterial = hitamMaterial;
            return;
        }

        // Jika display adalah GameObject, coba cari Renderer yang sesuai
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
