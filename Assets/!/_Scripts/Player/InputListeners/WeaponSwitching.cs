using UnityEngine;
using TMPro;
using EMullen.PlayerMgmt;
using System;

public class WeaponSwitching : MonoBehaviour
{
    public int selectedWeapon = 0;
    public TextMeshProUGUI GunText; // Reference to UI text that shows selected weapon name

    // Use this for initialization
    void Start()
    {
        SelectWeapon();
    }

    // Update is called once per frame
    void Update()
    {
        int previousSelectedWeapon = selectedWeapon;

        int scrollDir = Math.Sign(Input.GetAxis("Mouse ScrollWheel"));
        selectedWeapon += scrollDir;
        if(selectedWeapon > transform.childCount-1)
            selectedWeapon = 0;
        else if(selectedWeapon < 0)
            selectedWeapon = transform.childCount-1;

        // Number key input
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedWeapon = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && transform.childCount >= 2)
        {
            selectedWeapon = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && transform.childCount >= 3)
        {
            selectedWeapon = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) && transform.childCount >= 4)
        {
            selectedWeapon = 3;
        }

        // Only switch if weapon changed
        if (previousSelectedWeapon != selectedWeapon)
        {
            SelectWeapon();
        }
    }

    void SelectWeapon()
    {
        // Change active state
        for(int i = 0; i < transform.childCount; i++) {
            GameObject child = transform.GetChild(i).gameObject;
            bool isActive = i == selectedWeapon;
            child.SetActive(isActive);
        }

        Weapon weapon = GetWeapon();

        if (GunText != null)
            GunText.text = weapon.name;

        // Update player's InRoundData.gun so the HUD can read it
        Player player = GetComponentInParent<Player>();
        if (player != null && PlayerDataRegistry.Instance.Contains(player.uid.Value)) {
            PlayerData playerData = PlayerDataRegistry.Instance.GetPlayerData(player.uid.Value);
            InRoundData data = playerData.GetData<InRoundData>();
            data.gun = weapon.name;
            playerData.SetData(data);
        }
    }

    public Weapon GetWeapon() {
        try {
            return transform.GetChild(selectedWeapon).gameObject.GetComponent<Weapon>();
        } catch(UnityException e) {
            Debug.LogError($"Failed to get weapon at index {selectedWeapon}: " + e.Message);
        }
        return null;
    }
}
