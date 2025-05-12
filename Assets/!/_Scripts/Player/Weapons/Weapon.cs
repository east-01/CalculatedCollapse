using UnityEngine;

public class Weapon : MonoBehaviour 
{
    public int MaxUses = 10;
    public int Uses { get; protected set; }
    public float ReloadTime = 1f;
    public float UseRate = 1 / 15f; // Time in seconds per use

    public Sprite uiImage;
}