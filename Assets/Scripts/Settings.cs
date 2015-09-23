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

public class Settings
{
  public const string gameType = "Pillars";

  public const float BLOCK_SIZE = 1;
  public const int CHUNK_SIZE = 16;
  public const int chunkRenderDistance = 4;
  public const int chunkUnRenderDistance = chunkRenderDistance + 4;


  public static bool paused = false;
  public static float gravity = -9.8f;

  public static bool cursorLocked = false;

  public static string username = "";

  public static void ToggleCursor()
  {
    SetCursor(!cursorLocked);
  }
  public static void SetCursor(bool disable)
  {
    if (!disable)
    { 
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
      cursorLocked = false;
    }
    else
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
      cursorLocked = true;
    }
  }
}
