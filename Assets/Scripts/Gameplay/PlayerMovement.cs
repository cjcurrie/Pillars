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


public class PlayerMovement : MonoBehaviour
{ 
  float moveSpeed = 12;     // Meters per second
  float acceleration = 1;   // Seconds to reach top speed
  float turnSpeed = 20;     // ???
  float jumpSpeed = 15;      // How fast the player will gain height
  float airTime = .7f;        // Seconds the player will be gaining height from jumping
  float minAirTime = .3f;

  float jumpStartTime, fallingStartTime;
  public Vector3 velocity;
  private float trueAcceleration;
  float verticalSpeed;
  bool jumping, falling;

  // Cache
  GameObject myObj;
  private CharacterController controller;
  private Transform myTrans;
  Transform camTrans;

  public void UpdateMovement ()   // GameController 
  {
    if (Settings.paused || !myObj.activeSelf)
      return;

    float t = 0;
    bool wasGrounded = controller.isGrounded;

    // Jumping
    if (jumping)
    {
      t = Time.time - jumpStartTime;
      float jumpFactor = -( ((2f/airTime)*t) * ((2f/airTime)*t) ) + 1;
      verticalSpeed = jumpFactor * jumpSpeed;
    }
    else if (!falling)
      verticalSpeed = Settings.gravity;

    // Input
    Vector3 input = InputController.GetMoveInput();
    input = camTrans.right * input.x * moveSpeed
            + Vector3.up * verticalSpeed
            + camTrans.forward * input.z * moveSpeed;

    // Move
    velocity = Vector3.Lerp(velocity, input, Time.deltaTime * trueAcceleration);
    controller.Move(velocity * Time.deltaTime);

    // Look
    Vector3 look = new Vector3(input.x, 0, input.z);
    if (look != Vector3.zero)
      myTrans.rotation = Quaternion.Slerp(myTrans.rotation, Quaternion.LookRotation(look), Time.deltaTime*turnSpeed);
  
    // Falling/jumping into landing
    if (controller.isGrounded)
    {
      if (jumping && t > minAirTime)
        jumping = false;
      if (falling)
        falling = false;
    }
    // Landed into falling begins
    else if (wasGrounded)
    {
      fallingStartTime = Time.time;
      falling = true;
    }
    else if (falling)
    {
      t = Time.time - fallingStartTime + 1;
      verticalSpeed = Settings.gravity * (t*t);
    }
  }

  void OnJump()
  {
    if (!controller.isGrounded)
      return;

    jumpStartTime = Time.time;
    jumping = true;
  }

  public void Initialize()
  {
    myObj = gameObject;
    camTrans = GameController.camTrans;
    myTrans = GameController.playerTrans;

    velocity = Vector3.zero;
    controller = GetComponent<CharacterController>();
    trueAcceleration = moveSpeed / acceleration;
    InputController.RegisterKeyDownEvent(MovementKeys.Jump, OnJump);

    verticalSpeed = -10;
  }

  void DeInitialize()
  {
    InputController.UnRegisterKeyDownEvent(MovementKeys.Jump, OnJump);
  }
}
