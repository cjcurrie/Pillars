/*
 * Copyright (c) 2015 Colin James Currie.
 * All rights reserved.
 * Contact: cj@cjcurrie.net
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
  // === Public ===
  public bool runScene = true, skipLogin = false;
  public GameObject camera,
                    //player,
                    chunks;
  public static string seed;

  // === Private ===
  public static bool initialized, networkInitialized, applicationHasFocus;

  // === Cache ===
  static GameController instance;

  public static CameraController cameraController;
  public static Camera cam;
  public static Transform camTrans, cameraPivotTrans;

  public static GameObject player;
  public static PlayerMovement playerMovement;
  public static Transform playerTrans;
  public static PlayerUI playerUI;
  public static PlayerActions playerActions;
  public static NetworkBus networkBus;      // The networkBus is on the player

  public static NetworkController networkController;

  public static ChatUI chatUI;

  public static ChunkController chunkController;


  void Awake(){
    instance = this;
    PreInitialize();

    if (skipLogin)
    {
      Settings.username = "unnamed";
      NetworkController.HostP2PServer("default");
    }
  }

  void PreInitialize()
  {
    InputController.Initialize();

    networkController = GetComponent<NetworkController>();
    networkController.Initialize();
    chunkController = chunks.GetComponent<ChunkController>();

    chatUI = GetComponent<ChatUI>();
    chatUI.Initialize();
  }

  void Initialize()
  {
    initialized = true;

    if (camera)
    {
      cameraController = camera.GetComponent<CameraController>();
      cameraPivotTrans = camera.transform;
      cam = Camera.main;
      camTrans = cam.transform;
    }

    if (player)
    {
      playerTrans = player.transform;
      playerMovement = player.GetComponent<PlayerMovement>();
      playerUI = player.GetComponent<PlayerUI>();
      playerActions = player.GetComponent<PlayerActions>();
      networkBus = player.GetComponent<NetworkBus>();
    }
    else
      Debug.LogError("No player object found at Initialize()");
  }

  // Called from NetworkBus.Start() (if server) or NetworkController.OnSeedReceived (if client)
  public static void OnNetworkInitialized()   
  {
    instance.Initialize();
    instance.InitializeOthers();
  }

  void InitializeOthers()
  {
    if (camera)
    {
      cameraController.Initialize();
    }

    if (player)
    {
      networkBus.Initialize();
      playerMovement.Initialize();
      playerUI.Initialize();
      playerActions.Initialize();
    }
  }

  public static void OnSeedReceived(string s)
  {
    networkInitialized = true;

    if (s == "")
    {
      seed = "12345678";
      //seed = ""+Random.Range(0,9)+Random.Range(0,9)+Random.Range(0,9)+Random.Range(0,9)
      //  +Random.Range(0,9)+Random.Range(0,9)+Random.Range(0,9)+Random.Range(0,9);
      Random.seed = System.Int32.Parse(seed);
      //SimplexNoise simplex = new SimplexNoise(seed);
    }
    else
      seed = s;

    ChatUI.SystemMessage("The seed for this level is "+seed);

    chunkController.Initialize(seed);
  }

  void Update()
  {
    if (!networkInitialized)
      return;

    if (runScene)
    {
      if (!Settings.paused && applicationHasFocus)
      {
        InputController.UpdateMoveKeyDownEvents();
        InputController.UpdateCameraKeyDownEvents();
        InputController.UpdateActionKeyDownEvents();
        InputController.UpdateChatKeyDownEvents();

        playerActions.UpdatePlayerActions();

        // --- <Test> ---
        if (Input.GetKeyDown(KeyCode.RightShift))
          playerTrans.position = new Vector3(0,60,0);
      }
      
      chunkController.UpdateChunkController();
    }

    // --- Network ---

  }

  void FixedUpdate ()
  {
    if (!networkInitialized)
      return;

    if (runScene)
    {
      playerMovement.UpdateMovement();
        // Camera pos is based on player pos, so cameraController should update after playerMovement
      cameraController.UpdateCameraControls();
    }
  }

  void OnGUI()
  {
    chatUI.OnMyGUI();

    if (!networkInitialized)
      return;

    if (runScene)
    {
      playerUI.OnMyGUI();
    }
  }

  void OnApplicationQuit(){
    networkController.OnMyApplicationQuit();
  }
  void OnApplicationFocus(bool isIt)
  {
    applicationHasFocus = isIt;
  }
}
