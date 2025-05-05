using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

/// <summary>
/// The PlayerHUDMenuController class is responsible for controlling everything in the player's
///   pov.
/// </summary>
public class PlayerHUDMenuController : MenuController
{

    public static readonly string SUB_MENU_ACTIVE = "active";
    public static readonly string SUB_MENU_PAUSE = "pause";

    [SerializeField]
    private CanvasGroup activeHolder;
    public CanvasGroup ActiveHolder => activeHolder;
    [SerializeField]
    private GameObject scoreboard;
    [SerializeField]
    private TMP_Text winText;
    [SerializeField]
    private TMP_Text oppWinText;

    [SerializeField]
    private HealthBar healthBar;
    [SerializeField]
    private Crosshair crosshair;

    private Player player;

    protected new void Awake()
    {
        base.Awake();
    
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
    }

    private void Update()
    {
        if(!IsOpen)
            return;

        if(player == null && player.uid.Value != null)
            return;

        // Check if we're in lobby and update visibility
        LobbyData? lobbyNullable = LobbyManager.Instance.LobbyData;
        if(!lobbyNullable.HasValue) {
            UpdateVisibility(null);
            return;
        }
        LobbyData lobby = lobbyNullable.Value;

        UpdateVisibility(lobby.stateTypeString);

        // Get player's data and show it to screen
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(player.uid.Value);
        InRoundData data = pd.GetData<InRoundData>();

        healthBar.Value = data.health;
        winText.text = data.wins.ToString();
        oppWinText.text = GetOppWins();

        if(Input.GetKeyDown(KeyCode.H))
            crosshair.ShowHitmarker();

        if(Input.GetKeyDown(KeyCode.Escape)) {
            GetSubMenu(SUB_MENU_PAUSE).Open();
            ActiveHolder.alpha = 0f;
        }

        // TODO: Update gun icon
    }

    /// <summary>
    /// Set UI elements to active/inactive based off of state.
    /// For example, in StateWarmup nothing except the crosshair and gun should be visible.
    /// </summary>
    /// <param name="stateString">The current state type string from LobbyData</param>
    private void UpdateVisibility(string stateString) 
    {
        // TODO: This can probably be written wayyy better, but waiting for our ui to be more 
        //   fleshed out
        if(stateString == typeof(StateWarmup).ToString()) {
            healthBar.gameObject.SetActive(false);
            scoreboard.SetActive(false);
        } else if(stateString == typeof(StateInRound).ToString()) {
            healthBar.gameObject.SetActive(true);
            scoreboard.SetActive(true);
        } else if(stateString == typeof(StatePostRound).ToString()) {
            healthBar.gameObject.SetActive(false);
            scoreboard.SetActive(true);
        } else {
            healthBar.gameObject.SetActive(false);
            scoreboard.SetActive(false);
        }
    }

    /// <summary>
    /// Get the amount of wins that the opposition has, if they exist otherwise 
    /// </summary>
    /// <returns></returns>
    private string GetOppWins(string def = "-") 
    {
        LobbyData? lobbyNullable = LobbyManager.Instance.LobbyData;
        if(!lobbyNullable.HasValue)
            return def;
        LobbyData lobby = lobbyNullable.Value;

        List<string> oppUIDs = lobby.playerUIDs.Except(new string[] {player.uid.Value}).ToList();

        if(oppUIDs.Count == 0 || oppUIDs.Count > 1) {
            if(oppUIDs.Count > 1) Debug.LogError("Only know how to handle one oppUID");
            return def;
        }

        string oppUID = oppUIDs[0];

        if(!PlayerDataRegistry.Instance.Contains(oppUID))
            return def;

        PlayerData oppPD = PlayerDataRegistry.Instance.GetPlayerData(oppUID);

        if(!oppPD.HasData<InRoundData>())
            return def;

        InRoundData oppData = oppPD.GetData<InRoundData>();
        return oppData.wins.ToString();
    }

}
