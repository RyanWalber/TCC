using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonBones;

public class PlayAnimationbyTriggerkey : MonoBehaviour
{

public Button triggerButton;
public DragonBones.UnityArmatureComponent armatureUtama;
public string animationName;
public string nextAnimationName;
public KeyCode triggerKey;

void Start()
{
    triggerButton.onClick.AddListener(PlayAnimation);
}

void Update()
{
    if (Input.GetKeyDown(triggerKey))
    {
        PlayAnimation();
    }
}

void PlayAnimation()
{
    if (!string.IsNullOrEmpty(animationName))
    {
        var animationState = armatureUtama.animation.Play(animationName, 1);

        armatureUtama.AddDBEventListener(DragonBones.EventObject.COMPLETE, OnAnimationComplete);
    }
    else
    {
        Debug.LogError("Animation name is empty.");
    }
}

private void OnAnimationComplete(string type, DragonBones.EventObject eventObject)
{
    if (eventObject.animationState.name == animationName)
    {
        if (!string.IsNullOrEmpty(nextAnimationName))
        {
            armatureUtama.animation.Play(nextAnimationName);
        }
        else
        {
            Debug.LogError("Next animation name is empty.");
        }

        armatureUtama.RemoveDBEventListener(DragonBones.EventObject.COMPLETE, OnAnimationComplete);
    }
}


}
