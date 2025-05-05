using UnityEngine;

public class Crosshair : MonoBehaviour 
{
    [SerializeField]
    private CanvasGroup hitmarkerGroup;
    [SerializeField]
    private float hitmarkerTime = 0.1f;

    private float hitmarkerStartTime;

    private void Awake() 
    {
        hitmarkerStartTime = float.MinValue;
    }

    private void Update()
    {
        if(Time.time - hitmarkerStartTime > hitmarkerTime) {
            hitmarkerGroup.alpha = 0;
            return;   
        }

        float progress = 1 - ((Time.time - hitmarkerStartTime) / hitmarkerTime);
        hitmarkerGroup.alpha = progress;
    }

    public void ShowHitmarker() => hitmarkerStartTime = Time.time;
}