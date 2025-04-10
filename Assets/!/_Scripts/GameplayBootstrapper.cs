using System.Collections;
using System.Collections.Generic;
using EMullen.Bootstrapper;
using EMullen.Core;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

/// <summary>
/// The GameplayBootstrapper class is responsible for ensuring the lobby exists, the player is in
///   it, and is in the proper scene. You can see the phases in the GameplayBootstrapper#Status enum.
/// </summary>
public class GameplayBootstrapper : MonoBehaviour, IBootstrapComponent
{

    [SerializeField]
    private TMP_Text statusText;

    private Status status;
    [SerializeField]
    private float connectCooldown = 10f;

    private Dictionary<Status, string> statusMessages = new() {
        { Status.NONE, "None"},
        { Status.CONNECTING, "Connecting..." },
        { Status.JOINING_REGISTRY, "Synchronizing data..." },
        { Status.COMPLETE, "Done."}
    };

    public bool IsLoadingComplete() => status == Status.COMPLETE;

    private void Awake() {
        status = Status.NONE;
    }

    private void LateUpdate()
    {
        // BLog.Highlight($"Client active: {NetworkController.Instance.IsClientActive} Conn state: {NetworkController.Instance.ClientConnectionState == LocalConnectionState.Stopped} Server active: {NetworkController.Instance.IsServerActive} Server state: {NetworkController.Instance.ServerConnectionState == LocalConnectionState.Stopped}");
        if(!NetworkController.Instance.IsNetworkRunning()/* && Time.time-lastConnectTime >= connectCooldown*/) { 
            status = Status.CONNECTING;
            // BLog.Highlight($"Client: {NetworkController.Instance.IsClientActive} Server: {NetworkController.Instance.IsServerActive}");
            if(NetworkController.Instance.NetworkConfig == null && !NetworkController.Instance.NetworkConfiguratorMenuController.IsOpen) {
                NetworkController.Instance.NetworkConfiguratorMenuController.Open();
            } else if(NetworkController.Instance.CanStartNetwork()) {
                NetworkController.Instance.StartNetwork();
            }
        } else if(!PlayerDataRegistry.Instance.Networked) {
            status = Status.JOINING_REGISTRY;
        } else if(status != Status.COMPLETE) {
            status = Status.COMPLETE;

            BootstrapSequenceManager.Instance.AbortSequence();

            if(InstanceFinder.IsServerStarted) {

                SceneController.Instance.LoadServerScene(new("GameplayScene"));
                SceneController.Instance.AddClientToScene(InstanceFinder.ClientManager.Connection, new("GameplayScene"));
            } else {
                ClientNetworkedScene request = new(new SceneLookupData("GameplayScene"), ClientNetworkedScene.Action.ADD);
                InstanceFinder.ClientManager.Broadcast(request);
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        statusText.text = statusMessages[status];
    }

    public enum Status { NONE, CONNECTING, JOINING_REGISTRY, COMPLETE }
}
