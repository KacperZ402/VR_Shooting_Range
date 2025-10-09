using UnityEngine;

public class SimpleAnimationPlayer : MonoBehaviour
{
    public Animation animationComponent;

    public void PlayOnce(string clipName)
    {
        if (animationComponent != null && animationComponent.GetClip(clipName) != null)
            animationComponent.Play(clipName);
    }
}
