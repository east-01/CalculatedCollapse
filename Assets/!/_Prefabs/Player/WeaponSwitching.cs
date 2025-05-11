using UnityEngine;
using TMPro;
using EMullen.PlayerMgmt;

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

        // Scroll up
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            if (selectedWeapon <= 0)
                selectedWeapon = transform.childCount - 1;
            else
                selectedWeapon--;
        }
        // Scroll down
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            if (selectedWeapon >= transform.childCount - 1)
                selectedWeapon = 0;
            else
                selectedWeapon++;
        }

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
        int i = 0;
        foreach (Transform weapon in transform)
        {
            bool isActive = (i == selectedWeapon);
            weapon.gameObject.SetActive(isActive);

            if (isActive)
            {
                if (GunText != null)
                {
                    GunText.text = weapon.name;
                }

                // Update player's InRoundData.gun so the HUD can read it
                Player player = GetComponentInParent<Player>();
                if (player != null && PlayerDataRegistry.Instance.Contains(player.uid.Value))
                {
                    var playerData = PlayerDataRegistry.Instance.GetPlayerData(player.uid.Value);
                    var data = playerData.GetData<InRoundData>();
                    data.gun = weapon.name;
                }
            }

            i++;
        }
    }
}
