using UnityEngine;
using UnityEngine.UI; // Untuk akses ke UI
using DragonBones;

public class ChangeCapeSkinFromAnotherArmature : MonoBehaviour
{
    // Referensi ke armature utama dan armature sumber skin
    public UnityArmatureComponent armatureUtama; // Armature 'Utama'
    public UnityArmatureComponent armatureSkin;  // Armature 'SKIN'

    // Nama slot dan display
    public string capeSlotName = "CAPE";  // Nama slot untuk CAPE
    public string skinSlotName = "CAPE";   // Asumsikan slot di armature 'SKIN' juga bernama 'CAPE'

    // Referensi ke Button UI
    public Button changeSkinButton;

    void Start()
    {
        // Tambahkan listener ke button untuk memanggil metode ReplaceCapeSkin saat button di klik
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.AddListener(ReplaceCapeSkin);
        }
    }

    void ReplaceCapeSkin()
    {
        // Dapatkan slot dari armature 'Utama'
        Slot capeSlot = armatureUtama.armature.GetSlot(capeSlotName);

        // Dapatkan slot dari armature 'SKIN'
        Slot skinSlot = armatureSkin.armature.GetSlot(skinSlotName);

        if (capeSlot != null && skinSlot != null)
        {
            // Dapatkan display dari slot di armature 'SKIN'
            var newDisplay = skinSlot.display;

            // Ganti display slot di armature 'Utama' dengan display dari armature 'SKIN'
            capeSlot.display = newDisplay;

            // Sinkronkan transform slot dari 'SKIN' ke 'Utama'
            capeSlot.offset.x = skinSlot.offset.x;
            capeSlot.offset.y = skinSlot.offset.y;
            capeSlot.offset.scaleX = skinSlot.offset.scaleX;
            capeSlot.offset.scaleY = skinSlot.offset.scaleY;
            capeSlot.offset.rotation = skinSlot.offset.rotation;

            // Memaksa pembaruan armature untuk memastikan perubahan tampilan diterapkan
            armatureUtama.armature.AdvanceTime(0); 
        }
        else
        {
            Debug.LogError("Slot not found in one or both armatures.");
        }
    }

    
}
