using UnityEngine;
using UnityEngine.UI;

public class AnimationTrigger : MonoBehaviour
{
    public Button triggerButton; // Referensi ke tombol yang akan diklik
    public DragonBones.UnityArmatureComponent armatureUtama; // Referensi ke armature yang ingin Anda animasikan
    public string animationName; // Nama animasi yang ingin dimainkan

    void Start()
    {
        // Pastikan ada referensi ke tombol dan tambahkan listener untuk menangani klik
        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(PlayAnimation);
        }
    }

    void PlayAnimation()
    {
        // Memulai animasi pada armature yang dipilih
        var animationState = armatureUtama.animation.FadeIn(animationName, 0.2f);
        if (animationState != null)
        {
            animationState.Play();
        }
    }
}
