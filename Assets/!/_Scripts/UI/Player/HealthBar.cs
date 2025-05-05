using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///
public class HealthBar : MonoBehaviour
{

    private RectTransform selfTransform;
    [SerializeField]
    private RectTransform childTransform;

    [SerializeField]
    private float value = 1f;
    public float Value { 
        get => value;
        set => UpdateValue(value); 
    }

    private void Awake()
    {
        selfTransform = GetComponent<RectTransform>();   
    }

    private void UpdateValue(float val) 
    {
        this.value = Mathf.Clamp01(val);

        Vector2 sizeDelta = selfTransform.sizeDelta;
        sizeDelta.x *= this.value;

        childTransform.sizeDelta = sizeDelta;
    }
}

