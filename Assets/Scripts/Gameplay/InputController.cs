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

public enum MovementKeys {None, Forward, Backward, Left, Right, Jump};
public enum CameraKeys {None, LockCursor, LookUp, LookDown, LookLeft, LookRight};
public enum ActionKeys {None, Hotbar1, Hotbar2, Hotbar3, Hotbar4};
public enum ChatKeys {None, Return, HideChat};

public class InputController
{
  // === Delegates for input events ===
    public delegate void OnKeyDownEvent();
    
    // --- Movement events ---
    static KeyCode[] moveKeyBindings;
    static Dictionary<MovementKeys, OnKeyDownEvent> moveEvents;
    public static void RegisterKeyDownEvent(MovementKeys k, OnKeyDownEvent e) {
      if (!moveEvents.ContainsKey(k))
        moveEvents[k] = e;
      else
        moveEvents[k] += e;
    }
    public static void UnRegisterKeyDownEvent(MovementKeys k, OnKeyDownEvent e) {moveEvents[k] -= e;}

    // --- Camera events ---
    static KeyCode[] cameraKeyBindings;
    static Dictionary<CameraKeys, OnKeyDownEvent> cameraEvents;
    public static void RegisterKeyDownEvent(CameraKeys k, OnKeyDownEvent e) {
      if (!cameraEvents.ContainsKey(k))
        cameraEvents[k] = e;
      else
        cameraEvents[k] += e;
    }
    public static void UnRegisterKeyDownEvent(CameraKeys k, OnKeyDownEvent e) {cameraEvents[k] -= e;}
    
    // --- Action events ---
    static KeyCode[] actionKeyBindings;
    static Dictionary<ActionKeys, OnKeyDownEvent> actionEvents;
    public static void RegisterKeyDownEvent(ActionKeys k ,OnKeyDownEvent e){
      if (!actionEvents.ContainsKey(k))
        actionEvents[k] = e;
      else
        actionEvents[k] += e;
    }
    public static void UnRegisterKeyDownEvent(ActionKeys k, OnKeyDownEvent e) {actionEvents[k] -= e;}

    // --- Chat events ---
    static KeyCode[] chatKeyBindings;
    static Dictionary<ChatKeys, OnKeyDownEvent> chatEvents;
    public static void RegisterKeyDownEvent(ChatKeys k ,OnKeyDownEvent e){
      if (!chatEvents.ContainsKey(k))
        chatEvents[k] = e;
      else
        chatEvents[k] += e;
    }
    public static void UnRegisterKeyDownEvent(ChatKeys k, OnKeyDownEvent e) {chatEvents[k] -= e;}
  // === /Delegates ===


  public static void Initialize()
  {
    moveEvents = new Dictionary<MovementKeys, OnKeyDownEvent>();
    cameraEvents = new Dictionary<CameraKeys, OnKeyDownEvent>();
    actionEvents = new Dictionary<ActionKeys, OnKeyDownEvent>();
    chatEvents = new Dictionary<ChatKeys, OnKeyDownEvent>();

    
    InitializeKeyBindings();
  }

  // === These update methods are called from GameController.Update()
  public static void UpdateMoveKeyDownEvents()
  {
    foreach (MovementKeys k in moveEvents.Keys)
    {
      if (Input.GetKeyDown(moveKeyBindings[(int)k]))
        moveEvents[k]();
    }
  }

  public static void UpdateCameraKeyDownEvents()
  {
    foreach (CameraKeys k in cameraEvents.Keys)
    {
      if (Input.GetKeyDown(cameraKeyBindings[(int)k]))
        cameraEvents[k]();
    }
  }

  public static void UpdateActionKeyDownEvents()
  {
    foreach (ActionKeys k in actionEvents.Keys)
    {
      if (Input.GetKeyDown(actionKeyBindings[(int)k]))
        actionEvents[k]();
    }
  }

  public static void UpdateChatKeyDownEvents()
  {
    foreach(ChatKeys k in chatEvents.Keys)
    {
      if (Input.GetKeyDown(chatKeyBindings[(int)k]))
        chatEvents[k]();
    }
  }


  static void InitializeKeyBindings()
  {
    // Movement
    moveKeyBindings = new KeyCode[Enum.GetNames(typeof(MovementKeys)).Length];
    moveKeyBindings[(int)MovementKeys.Forward] = KeyCode.W;
    moveKeyBindings[(int)MovementKeys.Backward] = KeyCode.S;
    moveKeyBindings[(int)MovementKeys.Left] = KeyCode.A;
    moveKeyBindings[(int)MovementKeys.Right] = KeyCode.D;
    moveKeyBindings[(int)MovementKeys.Jump] = KeyCode.Space;

    // Camera
    cameraKeyBindings = new KeyCode[Enum.GetNames(typeof(CameraKeys)).Length];
    cameraKeyBindings[(int)CameraKeys.LockCursor] = KeyCode.LeftShift;
    cameraKeyBindings[(int)CameraKeys.LookUp] = KeyCode.UpArrow;
    cameraKeyBindings[(int)CameraKeys.LookDown] = KeyCode.DownArrow;
    cameraKeyBindings[(int)CameraKeys.LookLeft] = KeyCode.LeftArrow;
    cameraKeyBindings[(int)CameraKeys.LookRight] = KeyCode.RightArrow;

    // Action keys
    actionKeyBindings = new KeyCode[Enum.GetNames(typeof(ActionKeys)).Length];
    actionKeyBindings[(int)ActionKeys.Hotbar1] = KeyCode.Alpha1;
    actionKeyBindings[(int)ActionKeys.Hotbar2] = KeyCode.Alpha2;
    actionKeyBindings[(int)ActionKeys.Hotbar3] = KeyCode.Alpha3;
    actionKeyBindings[(int)ActionKeys.Hotbar4] = KeyCode.Alpha4;

    // Chat keys
    chatKeyBindings = new KeyCode[Enum.GetNames(typeof(ChatKeys)).Length];
    chatKeyBindings[(int)ChatKeys.Return] = KeyCode.Return;
    chatKeyBindings[(int)ChatKeys.HideChat] = KeyCode.Backslash;
  }


  // === PlayerActions ===
  public static bool DigInput()
  {return Input.GetMouseButtonDown(0);}
  public static bool BuildInput()
  {return Input.GetMouseButtonDown(1);}

  // === PlayerMovement ===
  public static Vector3 GetMoveInput()
  {
    Vector3 movement = Vector3.zero;

    if (Input.GetKey(moveKeyBindings[(int)MovementKeys.Forward]))
      movement += Vector3.forward;
    if (Input.GetKey(moveKeyBindings[(int)MovementKeys.Backward]))
      movement -= Vector3.forward;
    if (Input.GetKey(moveKeyBindings[(int)MovementKeys.Right]))
      movement += Vector3.right;
    if (Input.GetKey(moveKeyBindings[(int)MovementKeys.Left]))
      movement -= Vector3.right;

    return movement.normalized;
  }

  // === CameraController ===
  public static Vector2 GetLookInput()
  {
    Vector2 rotation = Vector2.zero;

    if (Input.GetKey(cameraKeyBindings[(int)CameraKeys.LookUp]))
      rotation += Vector2.up;
    if (Input.GetKey(cameraKeyBindings[(int)CameraKeys.LookDown]))
      rotation -= Vector2.up;
    if (Input.GetKey(cameraKeyBindings[(int)CameraKeys.LookRight]))
      rotation += Vector2.right;
    if (Input.GetKey(cameraKeyBindings[(int)CameraKeys.LookLeft]))
      rotation -= Vector2.right;

    if (rotation == Vector2.zero)
    {
      rotation = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    return rotation;
  }
}
