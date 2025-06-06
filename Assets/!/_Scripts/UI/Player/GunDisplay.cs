using TMPro;
using UnityEngine;

public class GunDisplay : MonoBehaviour 
{
    [SerializeField]
    private TMP_Text ammoText;
    [SerializeField]
    private TMP_Text gunNameText;
    [SerializeField]
    private UnityEngine.UI.Image gunImage;

    private Player player;
    private WeaponSwitching ws;
    private string shownWeaponName;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
        ws = player.GetComponentInChildren<WeaponSwitching>();
    }

    private void Update() 
    {
        if(ws == null)
            return;
            
        Weapon weapon = ws.GetWeapon();
        
        string ammoStr = $"{weapon.Uses} / {weapon.MaxUses}";
        if (weapon is Gun && (weapon as Gun).IsReloading)
            ammoStr = "Reloading...";

        ammoText.text = ammoStr;
        gunNameText.text = weapon.name;
        gunImage.sprite = weapon.uiImage;
    }
}