using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

public class GunDisplay : MonoBehaviour 
{
    [SerializeField]
    private TMP_Text ammoText;
    [SerializeField]
    private TMP_Text gunNameText;
    [SerializeField]
    private Image gunImage;

    private Player player;
    private GunRaycast gun;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
        gun = player.GetComponentInChildren<GunRaycast>();
    }

    private void Update() 
    {
        ammoText.text = $"{gun.CurrentAmmo} / {gun.maxAmmo}";
    }
}