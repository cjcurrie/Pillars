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

public class Chunk
{
  // Private
    int octaves, multiplier;
    float amplitude, lacunarity;

  // Public
    public int[] coord;
    public byte[,,] blocks;
    public bool rendering;
    public bool isDirty;

  // Cache
    // These three properties are set by ChunkController.LoadChunks()
    public GameObject myObj;
    public Transform myTrans;
    public ChunkRenderer chunkRenderer;


  public Chunk(int o, int m, float a, float l, int[] chunkCoord)
  {
    coord = chunkCoord;
    octaves=o;
    multiplier=m;
    amplitude=a;
    lacunarity=l;

    // Note: we over-generate by one in each axis in order to have chunk block overlap
    blocks = new byte[Settings.CHUNK_SIZE+1,Settings.CHUNK_SIZE+1,Settings.CHUNK_SIZE+1];
  }

  public void DigBlock(int[] blockCoords)
  {
    isDirty = true;

    blocks[blockCoords[0],blockCoords[1],blockCoords[2]] = (byte)BlockType.Air;
  }

  public void PlaceBlock(int[] blockCoords, BlockType type)
  {
    isDirty = true;

    blocks[blockCoords[0],blockCoords[1],blockCoords[2]] = (byte)type;
  }

  public void Generate(Vector3 offset, float worldHeight)
  {
    GenerateTerrain(offset, worldHeight);
    GenerateTrees(offset);
  }

  void GenerateTerrain(Vector3 offset, float worldHeight)
  {
    for(int x=0; x<blocks.GetLength(0); x++)
    {
      for(int y=0; y<blocks.GetLength(1); y++)
      {
        for(int z=0; z<blocks.GetLength(2); z++)
        {
          float height = (y+offset[1])/worldHeight;
          //float perturb = Random.Range(-.1f, .1f);

          float fractal = ChunkController.simplex.coherentNoise(x+offset.x, height+offset.y, z+offset.z,
                                        octaves, multiplier, amplitude, lacunarity);

          float total = height+fractal;

          if (total < .5f)
          {
            if (height>.6f)
              blocks[x,y,z] = (byte)BlockType.Grass;
            else if (height>.4f)
              blocks[x,y,z] = (byte)BlockType.Dirt;
            else if (height>.3f)
              blocks[x,y,z] = (byte)BlockType.Sand;
            else
              blocks[x,y,z] = (byte)BlockType.Stone;
          }
          else
            blocks[x,y,z] = (byte)BlockType.Air;
        }
      }
    }
  }

  void GenerateTrees(Vector3 offset)
  {
    int yHeight = blocks.GetLength(1);

    for(int x=0; x<blocks.GetLength(0); x++)
    {
      for(int y=0; y<yHeight; y++)
      {
        for(int z=0; z<blocks.GetLength(2); z++)
        {
          BlockType t = (BlockType)blocks[x,y,z];

          if ((t==BlockType.Grass || t==BlockType.Dirt))
          {
            float fractal = ChunkController.simplex.coherentNoise(x+offset.x, y+offset.y, z+offset.z,
                                        2, 5, 2, 4);

            if (fractal < .666f)
              continue;

            int treeLength = 4;//Random.Range(3,7);
            
            // Grow trunk
            int u;
            for (u=0; u<treeLength; u++)
            {
              if (u+y >= yHeight-1)
                break;

              blocks[x,y+u,z] = (byte)BlockType.Log;
            }

            // Draw leaves
            blocks[x,y+u,z] = (byte)BlockType.Leaves;
          }
        }
      }
    }
  }

  public bool SharesCoords(int[] otherCoord)
  {
    return coord[0] == otherCoord[0] && coord[1] == otherCoord[1] && coord[2] == otherCoord[2];
  }
}

class ChunkDistanceComparer : IComparer<float>
{
  public int Compare(float a, float b) {
    /*
    if (GameController.playerTrans == null)
      return 0;

    Vector3 pPos = GameController.playerTrans.position;
    Vector3 offset = Vector3.one * Settings.CHUNK_SIZE * Settings.BLOCK_SIZE / 2;
    Vector3 myPos = Util.CoordsToVector3(a.coord) + offset;
    Vector3 otherPos = Util.CoordsToVector3(b.coord) + offset;
    float myMag = (myPos-pPos).sqrMagnitude;
    float otherMag = (otherPos-pPos).sqrMagnitude;
  */
    if (a < b)
      return 1;
    else if (b < a)
      return -1;
    else
      return 0;
  }
}