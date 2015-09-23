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


public class PlayerActions : MonoBehaviour
{
  public GameObject blockDugEffectPrefab;

  float digDistance = 5;
  [HideInInspector] public float digDistSquared;
  BlockType heldBlock;

  Camera cam;
  Vector2 center;
  int layermask;
  Transform playerTrans;
  ChunkController chunkCont;

  public void Initialize()
  {
    cam = GameController.cam;
    playerTrans = GameController.playerTrans;
    chunkCont = GameController.chunkController;
    playerTrans.position = Vector3.up * 20;
    
    layermask = 1<<8;   // Layer 8 is set up as "Chunk" in the Tags & Layers manager
    digDistSquared = digDistance*digDistance;

    center = new Vector2(Screen.width/2, Screen.height/2);

    InputController.RegisterKeyDownEvent(ActionKeys.Hotbar1, OnHotbar1);
    InputController.RegisterKeyDownEvent(ActionKeys.Hotbar2, OnHotbar2);
    InputController.RegisterKeyDownEvent(ActionKeys.Hotbar3, OnHotbar3);
    InputController.RegisterKeyDownEvent(ActionKeys.Hotbar4, OnHotbar4);

    heldBlock = BlockType.Stone;
  }

  void OnHotbar1() {OnHotbar(1);}
  void OnHotbar2() {OnHotbar(2);}
  void OnHotbar3() {OnHotbar(3);}
  void OnHotbar4() {OnHotbar(4);}
  void OnHotbar(int id)
  {
    switch(id)
    {
      case 1:
        heldBlock = BlockType.Stone;
      break;
      case 2:
        heldBlock = BlockType.Sand;
      break;
      case 3:
        heldBlock = BlockType.Log;
      break;
      case 4:
        heldBlock = BlockType.Grass;
      break;
    }
  }

  public void UpdatePlayerActions()
  {
    if (InputController.DigInput())
    {
      RaycastHit hit;
      if (Physics.Raycast(cam.ScreenPointToRay(center), out hit, 50, layermask))
      {
        if ((hit.point-playerTrans.position).sqrMagnitude < digDistSquared)
        {
          Vector3 cubeCenter = hit.point - hit.normal*Settings.BLOCK_SIZE/4f;
          //int[] chunkCoord = GetChunkCoord(cubeCenter);

          //Debug.Log("Ray at "+cubeCenter+" and we picked block "+Util.CoordsToString(blockCoord)+" from chunk "+Util.CoordsToString(chunkCoord));

          GameController.networkBus.CmdTryDigBlock(cubeCenter);
        }
      }
    }

    if (InputController.BuildInput())
    {
      RaycastHit hit;
      if (Physics.Raycast(cam.ScreenPointToRay(center), out hit, 50, layermask))
      {
        if ((hit.point-playerTrans.position).sqrMagnitude < digDistSquared)
        {
          Vector3 cubeCenter = hit.point + hit.normal*Settings.BLOCK_SIZE/4f;
          
          GameController.networkBus.CmdTryPlaceBlock(cubeCenter, heldBlock);
        }
      }
    }
  }

  public void OnBlockDug(int[] chunkCoord, int[] blockCoord)
  {
    Vector3 point = Util.CoordsToVector3(chunkCoord, blockCoord) + Vector3.one*Settings.BLOCK_SIZE/2;

    GameObject effect = (GameObject)Instantiate(blockDugEffectPrefab, point, Quaternion.identity);
    Destroy(effect, 1);
  }
}
