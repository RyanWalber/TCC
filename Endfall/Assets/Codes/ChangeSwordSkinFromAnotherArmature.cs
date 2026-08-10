using UnityEngine;
using UnityEngine.UI; // Untuk akses ke UI
using DragonBones;

public class ChangeSwordSkinFromAnotherArmature : MonoBehaviour
{
    // Referensi ke armature utama dan armature sumber skin
    public UnityArmatureComponent armatureUtama; // Armature 'Utama'
    public UnityArmatureComponent armatureSkin;  // Armature 'SKIN'

    // Nama slot dan display
    public string swordSlotName = "SWORD";  // Nama slot untuk SWORD
    public string skinSlotName = "SWORD";   // Asumsikan slot di armature 'SKIN' juga bernama 'SWORD'

    // Referensi ke Button UI
    public Button changeSkinButton;

    void Start()
    {
        // Tambahkan listener ke button untuk memanggil metode ReplaceSwordSkin saat button di klik
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.AddListener(ReplaceSwordSkin);
        }
    }

    void ReplaceSwordSkin()
    {
        // Dapatkan slot dari armature 'Utama'
        Slot swordSlot = armatureUtama.armature.GetSlot(swordSlotName);

        // Dapatkan slot dari armature 'SKIN'
        Slot skinSlot = armatureSkin.armature.GetSlot(skinSlotName);

        if (swordSlot != null && skinSlot != null)
        {
            // Dapatkan display dari slot di armature 'SKIN'
            var newDisplay = skinSlot.display;

            // Ganti display slot di armature 'Utama' dengan display dari armature 'SKIN'
            swordSlot.display = newDisplay;

            // Sinkronkan transform slot dari 'SKIN' ke 'Utama'
            swordSlot.offset.x = skinSlot.offset.x;
            swordSlot.offset.y = skinSlot.offset.y;
            swordSlot.offset.scaleX = skinSlot.offset.scaleX;
            swordSlot.offset.scaleY = skinSlot.offset.scaleY;
            swordSlot.offset.rotation = skinSlot.offset.rotation;

            // Memaksa pembaruan armature untuk memastikan perubahan tampilan diterapkan
            armatureUtama.armature.AdvanceTime(0); 
        }
        else
        {
            Debug.LogError("Slot not found in one or both armatures.");
        }
    }

    void OnDestroy()
    {
        // Jangan lupa untuk menghapus listener saat GameObject dihancurkan
        if (changeSkinButton != null)
        {
            changeSkinButton.onClick.RemoveListener(ReplaceSwordSkin);
        }
    }
}
