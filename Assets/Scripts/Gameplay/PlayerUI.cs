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


public class PlayerUI : MonoBehaviour
{
  public Texture2D crosshairIcon;

  // Private rect cache
  Rect crosshairRect;

  public void Initialize()
  {
    crosshairRect = new Rect(Screen.width/2 - 25, Screen.height/2 -25, 50, 50);
  }

  public void OnMyGUI()
  {
    GUI.DrawTexture(crosshairRect, crosshairIcon);
  }
}
