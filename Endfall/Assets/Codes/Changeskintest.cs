using UnityEngine;
using UnityEngine.UI;
using DragonBones;

public class ReplaceAndReplaySlotDisplay : MonoBehaviour
{
    public UnityArmatureComponent armatureUtama; // Armature 'Utama'
    public UnityArmatureComponent armatureSkin;  // Armature 'SKIN'

    public string slotName = "SWORD";  // Nama slot yang akan diganti
    public string displayName = "Sword_Skin_01";  // Nama display baru yang diambil dari armature 'SKIN'
    public string animationName = "SWORD_Idle";  // Nama animasi yang akan diputar ulang

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
                // Simpan referensi ke display lama dan pastikan itu adalah GameObject
                var oldDisplay = targetSlot.display as GameObject;
                
                // Ganti display slot di armature 'Utama' dengan display dari armature 'SKIN'
                targetSlot.display = sourceSlot.display;

                // Cek apakah display baru adalah GameObject dan display lama tidak null
                if (targetSlot.display is GameObject newDisplay && oldDisplay != null)
                {
                    // Salin transformasi dari display lama ke display baru
                    newDisplay.transform.localPosition = oldDisplay.transform.localPosition;
                    newDisplay.transform.localRotation = oldDisplay.transform.localRotation;
                    newDisplay.transform.localScale = oldDisplay.transform.localScale;
                }

                // Memaksa pembaruan armature untuk memastikan perubahan tampilan diterapkan
                armatureUtama.armature.AdvanceTime(0);

                // Putar ulang animasi pada armature utama
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
