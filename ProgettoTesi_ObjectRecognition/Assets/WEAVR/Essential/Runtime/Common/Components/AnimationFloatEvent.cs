using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Obsolete("Use Animate By Value")]
[AddComponentMenu("")]
public class AnimationFloatEvent : MonoBehaviour
{

    public GameObject target;
    public AnimationClip animationClip;
    public float StartPointPerc;

    public void PlayAnimation(float progress)
    {
        if (animationClip && progress > StartPointPerc)
        {
            animationClip.SampleAnimation(target, animationClip.length * ((progress - StartPointPerc) / (1 - StartPointPerc)));
        }
    }
}
