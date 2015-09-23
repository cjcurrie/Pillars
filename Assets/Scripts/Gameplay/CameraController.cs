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


public class CameraController : MonoBehaviour
{
  // Const
  const string delegateKey = "camera";


  // Private
  float sensitivityX = 2, sensitivityY = 1.5f;
  float minimumX = -360F, maximumX = 360F, minimumY = -90F, maximumY = 90F;
  float rotationX, rotationY;
  Quaternion originalRotation;

  // Cache
  Transform myTrans, playerTrans, playerHeadTrans;

  public void Initialize()    // Called by GameController
  {
    Settings.SetCursor(true);

    myTrans = GameController.cameraPivotTrans;
    playerTrans = GameController.playerTrans;

    playerHeadTrans = playerTrans.Find("PlayerModel").Find("head").transform;

    originalRotation = myTrans.localRotation;
    //rotArrayX = new Queue<float>();
    //rotArrayY = new Queue<float>();

    // Register an input with our function
    InputController.RegisterKeyDownEvent(CameraKeys.LockCursor, Settings.ToggleCursor);
  }

  void DeInitialize()
  {
    InputController.UnRegisterKeyDownEvent(CameraKeys.LockCursor, Settings.ToggleCursor);
  }
  
  public void UpdateCameraControls()
  {
    if (Settings.paused || !Settings.cursorLocked)
      return;

    // --- Poll input ---
    Vector2 input = InputController.GetLookInput();
    //input = new Vector2(input.x*Mathf.Abs(input.x), input.y*Mathf.Abs(input.y));

    rotationX += input.x*sensitivityX;
    rotationY += input.y*sensitivityY;

    // --- Clamp values ---
    rotationX = ClampAngle (rotationX, minimumX, maximumX);
    rotationY = ClampAngle (rotationY, minimumY, maximumY);

    // --- Apply rotation ---
    Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
    Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, Vector3.left);
    myTrans.localRotation = originalRotation * xQuaternion * yQuaternion;
    //playerHeadTrans.rotation = myTrans.rotation;

    // --- Apply location ---
    myTrans.position = playerTrans.position+Vector3.up;
  }

  public static float ClampAngle (float angle, float min, float max)
  {
    if (angle < -360F)
      angle += 360F;
    if (angle > 360F)
      angle -= 360F;
    return Mathf.Clamp (angle, min, max);
  }
}
