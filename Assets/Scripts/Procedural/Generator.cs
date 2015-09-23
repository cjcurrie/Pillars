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


public class Generator : MonoBehaviour
{
  public class Effects
  {
    public static void Gradient(byte[,,] blocks)
    {
      
    }
  }

  public class Primitives
  {
    public static void Sphere(byte[,,] blocks)
    {
      int radius = Settings.CHUNK_SIZE / 2;
      int r2 = (int)(radius*radius);
      int[] center = new int[]{blocks.GetLength(0)/2,blocks.GetLength(1)/2,blocks.GetLength(2)/2};

      for(int x=0; x<blocks.GetLength(0); x++)
      {
          for(int y=0; y<blocks.GetLength(1); y++)
          {
              for(int z=0; z<blocks.GetLength(2); z++)
              {
                int dist = (x-center[0]) * (x-center[0]) + (y-center[1]) * (y-center[1]) + (z-center[2]) * (z-center[2]);

                if (dist < r2)    // Inside the sphere
                {
                  switch(Random.Range(0,2))
                  {
                    case 0:
                      blocks[x,y,z] = (byte)BlockType.Dirt;
                    break;
                    case 1:
                      blocks[x,y,z] = (byte)BlockType.Grass;
                    break;
                  }
                }
                else
                  blocks[x,y,z] = (byte)BlockType.Air;
              }
          }
      }
    }

    public static void PureRandom(byte[,,] blocks)
    {
      for(int i = 0; i<blocks.GetLength(0); i++)
      {
        for(int j = 0; j<blocks.GetLength(1); j++)
        {
            for(int k=0; k<blocks.GetLength(2); k++)
            {
              switch (Random.Range(0,6))
              {
                case 0:
                  blocks[i,j,k] = (byte)BlockType.Grass;
                break;
                case 1:
                  blocks[i,j,k] = (byte)BlockType.Dirt;
                break;

                default:
                  blocks[i,j,k] = (byte)BlockType.Air;
                break;
              }
            }
        }
      }
    }

    public static void Hash3D(byte[,,] blocks)
    {
      int quarter = Settings.CHUNK_SIZE/4;
      int threeQuarter = quarter * 3;

      for(int x=0; x<blocks.GetLength(0); x++)
      {
          for(int y=0; y<blocks.GetLength(1); y++)
          {
              for(int z=0; z<blocks.GetLength(2); z++)
              {
                if ((x==quarter||x==threeQuarter) || (y==quarter||y==threeQuarter) || (z==quarter||z==threeQuarter))
                  blocks[x,y,z] = (byte)BlockType.Grass;
                else
                  blocks[x,y,z] = (byte)BlockType.Air;
              }
          }
      }
    }

    public static void Cube(byte[,,] blocks)
    {
      int quarter = Settings.CHUNK_SIZE/4;
      int threeQuarter = quarter * 3;

      for(int x=0; x<blocks.GetLength(0); x++)
      {
          for(int y=0; y<blocks.GetLength(1); y++)
          {
              for(int z=0; z<blocks.GetLength(2); z++)
              {
                if (x>quarter && x<threeQuarter && y>quarter && y<threeQuarter && z>quarter && z<threeQuarter)
                {
                  switch (Random.Range(0,1))
                  {
                    case 0:
                      blocks[x,y,z] = (byte)BlockType.Grass;
                    break;
                    case 1:
                      blocks[x,y,z] = (byte)BlockType.Dirt;
                    break;
                  }
                }
                else
                  blocks[x,y,z] = (byte)BlockType.Air;
              }
          }
      }
    }
  }
}
