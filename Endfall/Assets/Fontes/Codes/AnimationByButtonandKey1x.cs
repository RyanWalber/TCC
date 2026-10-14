using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Tambahkan namespace ini untuk menggunakan UI components
using DragonBones;

public class PlayAnimationbyTriggerkey : MonoBehaviour
{
    
public Button triggerButton; // Referensi ke tombol yang akan diklik
public DragonBones.UnityArmatureComponent armatureUtama; // Referensi ke armature yang ingin Anda animasikan
public string animationName; // Nama animasi yang ingin dimainkan
public string nextAnimationName; // Nama animasi yang dimainkan setelah animasi pertama selesai
public KeyCode triggerKey; // Key dari keyboard yang akan digunakan untuk trigger animasi

void Start()
{
    // Menambahkan listener untuk tombol
    triggerButton.onClick.AddListener(PlayAnimation);
}

void Update()
{
    // Cek jika key dari keyboard ditekan
    if (Input.GetKeyDown(triggerKey))
    {
        PlayAnimation();
    }
}

void PlayAnimation()
{
    // Memastikan nama animasi tidak kosong
    if (!string.IsNullOrEmpty(animationName))
    {
        // Memainkan animasi yang ditentukan
        var animationState = armatureUtama.animation.Play(animationName, 1);

        // Menambahkan listener untuk event COMPLETE, agar memutar animasi selanjutnya setelah animasi pertama selesai
        armatureUtama.AddDBEventListener(DragonBones.EventObject.COMPLETE, OnAnimationComplete);
    }
    else
    {
        Debug.LogError("Animation name is empty.");
    }
}

// Callback ketika animasi selesai diputar
private void OnAnimationComplete(string type, DragonBones.EventObject eventObject)
{
    // Pastikan event ini berasal dari animasi yang dimainkan
    if (eventObject.animationState.name == animationName)
    {
        // Memainkan animasi berikutnya setelah animasi pertama selesai
        if (!string.IsNullOrEmpty(nextAnimationName))
        {
            armatureUtama.animation.Play(nextAnimationName);
        }
        else
        {
            Debug.LogError("Next animation name is empty.");
        }

        // Menghapus listener untuk mencegah pemanggilan callback yang tidak diinginkan
        armatureUtama.RemoveDBEventListener(DragonBones.EventObject.COMPLETE, OnAnimationComplete);
    }
}


}
