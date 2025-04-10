using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The GameplayManager class is a networked scene singleton that controls general gameplay actions.
/// It mainly controlls TreeLogGroups at the moment, but that should be split to its own manager soon.
/// </summary>
public class GameplayManager : NetworkBehaviour
{

    public static Dictionary<SceneLookupData, GameplayManager> SceneSingleton = new();

    public Scene GameplayScene => gameObject.scene;

    private void Start() 
    {
        SceneSingletons.Register(this);
    }

    private void OnEnable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData())) {
            Debug.LogError("GameplayManager Instance already exists, destroying.");
            return;
        }

        SceneSingleton.Add(gameObject.scene.GetSceneLookupData(), this);
    }

    private void OnDisable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData()))
            SceneSingleton.Remove(gameObject.scene.GetSceneLookupData());
    }

    private void Update() 
    {

    }

}
