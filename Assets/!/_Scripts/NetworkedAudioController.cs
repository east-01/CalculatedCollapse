using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using UnityEngine;

/// <summary>
/// This is a temporary library for networking audio, I plan on writing a much more robust one when
///   I have more time.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class NetworkedAudioController : NetworkBehaviour
{
    [SerializeField]
    private List<IDAudioClip> _audioClips;
    private Dictionary<string, AudioClip> audioClips;

    private AudioSource source;

    private void Awake() 
    {
        source = GetComponent<AudioSource>();

        audioClips = new();
        _audioClips.ForEach(idac => {
            if(audioClips.ContainsKey(idac.id)) {
                Debug.LogError($"Audio clips dict already has id \"{idac.id}\" duplicate ids are not allowed.");
                return;
            }
            audioClips.Add(idac.id, idac.audioClip);
        });
    }

    public void PlaySound(string soundID, bool propogate = true) 
    {
        if(!audioClips.ContainsKey(soundID)) {
            Debug.LogError($"Can't play sound \"{soundID}\"");
            return;
        }

        AudioClip sound = audioClips[soundID];

        source.clip = sound;
        source.Play();

        if(propogate)
            PropogateSound(soundID, new NetworkConnection[] {LocalConnection});
    }

    /// <summary>
    /// Propogates a AudioClip sound to all clients connected via TargetRPC
    /// </summary>
    private void PropogateSound(string sound, NetworkConnection[] blacklistedClients = null) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRpcPropogateSound(sound, blacklistedClients);
            return;
        }

        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            if(blacklistedClients != null && blacklistedClients.Contains(conn))
                continue;

            TargetRpcPropogateSound(conn, sound);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcPropogateSound(string sound, NetworkConnection[] blacklistedClients = null) => PropogateSound(sound, blacklistedClients);
    [TargetRpc]
    private void TargetRpcPropogateSound(NetworkConnection conn, string sound) => PlaySound(sound, false);

    [Serializable]
    public struct IDAudioClip 
    {
        public string id;
        public AudioClip audioClip;
    }

}