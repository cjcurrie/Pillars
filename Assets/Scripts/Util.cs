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


public class Util
{
  public static string CoordsToString(int[] coord)
  {
    return coord[0]+","+coord[1]+","+coord[2];
  }

  public static int[] GetChunkCoord(Vector3 p)
  {
    float scale = Settings.CHUNK_SIZE*Settings.BLOCK_SIZE;

    return new int[]{(int)Mathf.Floor(p.x/scale), (int)Mathf.Floor(p.y/scale), (int)Mathf.Floor(p.z/scale)};
  }

  public static int[] GetBlockCoord(Vector3 p, int[] chunkCoord)
  {
    float scale = Settings.CHUNK_SIZE*Settings.BLOCK_SIZE;

    return new int[]{(int)Mathf.Floor((p.x-chunkCoord[0]*scale)/Settings.BLOCK_SIZE),
            (int)Mathf.Floor((p.y-chunkCoord[1]*scale)/Settings.BLOCK_SIZE),
            (int)Mathf.Floor((p.z-chunkCoord[2]*scale)/Settings.BLOCK_SIZE)};
  }

  public static Vector3 CoordsToVector3(int[] chunkCoord, int[] blockCoord)
  {
    return (new Vector3(chunkCoord[0],chunkCoord[1],chunkCoord[2]) * Settings.CHUNK_SIZE + new Vector3(blockCoord[0],blockCoord[1],blockCoord[2]))*Settings.BLOCK_SIZE;
  }

  public static Vector3 CoordsToVector3(int[] chunkCoord)
  {
    return new Vector3(chunkCoord[0],chunkCoord[1],chunkCoord[2]) * Settings.CHUNK_SIZE * Settings.BLOCK_SIZE;
  }
}
