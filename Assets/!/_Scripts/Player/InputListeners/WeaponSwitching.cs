using UnityEngine;
using TMPro;
using EMullen.PlayerMgmt;
using System;

/// <summary>
/// WeaponSwitching allows the player to switch between weapon GameObjects
/// using mouse scroll or number keys, and updates UI and player data accordingly.
/// </summary>
public class WeaponSwitching : MonoBehaviour
{
    // Index of the currently selected weapon
    public int selectedWeapon = 0;

    // UI text element showing the current weapon's name
    public TextMeshProUGUI GunText;

    private void Start()
    {
        // Activate the initially selected weapon
        SelectWeapon();
    }

    private void Update()
    {
        int previousSelectedWeapon = selectedWeapon;

        // Scroll input to cycle weapons
        int scrollDir = Math.Sign(Input.GetAxis("Mouse ScrollWheel"));
        selectedWeapon += scrollDir;

        // Wrap around if index goes out of bounds
        if (selectedWeapon > transform.childCount - 1)
            selectedWeapon = 0;
        else if (selectedWeapon < 0)
            selectedWeapon = transform.childCount - 1;

        // Number key shortcuts
        if (Input.GetKeyDown(KeyCode.Alpha1))
            selectedWeapon = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) && transform.childCount >= 2)
            selectedWeapon = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) && transform.childCount >= 3)
            selectedWeapon = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) && transform.childCount >= 4)
            selectedWeapon = 3;

        // Switch weapon only if the selection has changed
        if (previousSelectedWeapon != selectedWeapon)
            SelectWeapon();
    }

    // Activates the selected weapon and updates UI and player data
    private void SelectWeapon()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            bool isActive = i == selectedWeapon;
            child.SetActive(isActive);
        }

        Weapon weapon = GetWeapon();

        if (GunText != null)
            GunText.text = weapon.name;

        // Sync weapon name to InRoundData for HUD access
        Player player = GetComponentInParent<Player>();
        if (player != null && PlayerDataRegistry.Instance.Contains(player.uid.Value))
        {
            PlayerData playerData = PlayerDataRegistry.Instance.GetPlayerData(player.uid.Value);
            InRoundData data = playerData.GetData<InRoundData>();
            data.gun = weapon.name;
            playerData.SetData(data);
        }
    }

    // Returns the currently selected weapon component
    public Weapon GetWeapon()
    {
        try
        {
            return transform.GetChild(selectedWeapon).gameObject.GetComponent<Weapon>();
        }
        catch (UnityException e)
        {
            Debug.LogError($"Failed to get weapon at index {selectedWeapon}: " + e.Message);
        }
        return null;
    }
}
