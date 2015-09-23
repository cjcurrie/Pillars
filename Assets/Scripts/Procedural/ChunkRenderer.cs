/*
 * Copyright (chunk) 2015 Colin James Currie.
 * All rights reserved.
 * Contact: cj@cjcurrie.net
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;


public class ChunkRenderer : MonoBehaviour
{
  Chunk chunk;

  const int updateTick = 6;
  int updateCounter;

  // Cache
  //Transform myTrans;
  Renderer myRend;
  MeshFilter myFilter;
  MeshCollider myCollider;

  public void Initialize(Chunk c)
  {
    chunk = c;

    //myTrans = transform;
    myRend = GetComponent<Renderer>();
    myFilter = GetComponent<MeshFilter>();
    myCollider = GetComponent<MeshCollider>();

    updateCounter = 0;
  }

  public void AutomaticRenderUpdate()
  {
    if (updateCounter > updateTick)
    {
      updateCounter = 0;
      if (chunk.isDirty)
      {
        ReRender();
      }
    }
    else
      updateCounter++;
  }


  public void ReRender()
  {
    DestroyImmediate(myFilter.sharedMesh);
    DestroyImmediate(myCollider.sharedMesh);
    Render();
    chunk.isDirty = false;
  }

  public void Render()
  {
    Queue<Vector3> vertices = new Queue<Vector3>();
    Queue<int> triangles = new Queue<int>();
    Queue<Vector3> normals = new Queue<Vector3>();
    Queue<Vector2>uvs = new Queue<Vector2>();

    float voxelScale = Settings.BLOCK_SIZE;
    Vector3 offset2 = Vector3.zero;   // This can be changed to alter the "center pivot" of the chunk

    int counter = 0;

    for (int x=0; x<chunk.blocks.GetLength(0); x++)
    {
      for (int y=0; y<chunk.blocks.GetLength(1); y++)
      {
        for (int z=0; z<chunk.blocks.GetLength(2); z++)
        {
          if (chunk.blocks[x,y,z] == (byte)BlockType.None)
            continue;

          float xPos = (float)(x*voxelScale) - offset2.x;
          float xOffset = (float)(xPos+voxelScale);
          float yPos = (float)(y*voxelScale) - offset2.y;
          float yOffset = (float)(yPos+voxelScale);
          float zPos = (float)(z*voxelScale) - offset2.z;
          float zOffset = (float)(zPos+voxelScale);
          Vector3 leftBotBack = new Vector3( xPos, yPos, zPos ),
                  leftBotFor = new Vector3( xPos, yPos, zOffset ),
                  leftTopBack = new Vector3( xPos, yOffset, zPos ),
                  leftTopFor = new Vector3( xPos, yOffset, zOffset ),
                  rightBotBack = new Vector3( xOffset, yPos, zPos ),
                  rightBotFor = new Vector3( xOffset, yPos, zOffset ),
                  rightTopBack = new Vector3( xOffset, yOffset, zPos ),
                  rightTopFor = new Vector3( xOffset, yOffset, zOffset );

          Vector3 normal;
          Vector2 uv1, uv2, uv3, uv4;

          // Note: Atlas faces (2nd index) are defined in ChunkController.InitializeTextures() as
          //  0=top 1=bottom 2=left 3=right 4=front 5=back

          // Note: Atlas UVs (3rd index) are defined in ChunkController.InitializeTextures()
          //  1=bottomLeft, 2=bottomRight, 3=topRight, 4=topLeft

          // === Render step is applied first to blocks adjacent to air blocks
          if (Block.attributes[chunk.blocks[x,y,z]].isTransparent)
          {
          // --- Check left ---
            if (x>0 && chunk.blocks[x-1,y,z] != (byte)BlockType.Air)
            {
              normal = Vector3.right;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x-1,y,z],3,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x-1,y,z],3,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x-1,y,z],3,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x-1,y,z],3,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopBack );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopFor );     // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotBack );    // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( leftBotFor );     // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // --- Check down ---
            if (y>0 && chunk.blocks[x,y-1,z] != (byte)BlockType.Air)
            {
              normal = Vector3.up;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y-1,z],0,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y-1,z],0,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y-1,z],0,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y-1,z],0,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightBotFor );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotBack );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotFor );     // Bottom Left
              normals.Enqueue( normal);
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( leftBotBack );    // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // --- Check backward ---
            if (z>0 && chunk.blocks[x,y,z-1] != (byte)BlockType.Air)
            {
              normal = Vector3.forward;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z-1],4,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z-1],4,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z-1],4,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z-1],4,3];
            
              vertices.Enqueue( rightTopBack );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( leftTopBack );    // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( rightBotBack );   // Bottom left
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);

              // Second trianglenter++;
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( leftBotBack );   // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // --- Check right ---
            if (x<chunk.blocks.GetLength(0)-1 && chunk.blocks[x+1,y,z] != (byte)BlockType.Air)
            {
              normal = -Vector3.right;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x+1,y,z],2,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x+1,y,z],2,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x+1,y,z],2,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x+1,y,z],2,3];
              
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightTopFor );      // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopBack );     // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotFor );      // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);           // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);           // Top right
              counter += 2;

              vertices.Enqueue( rightBotBack );     // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // --- Check up ---
            if (y<chunk.blocks.GetLength(1)-1 && chunk.blocks[x,y+1,z] != (byte)BlockType.Air)
            {
              normal = -Vector3.up;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y+1,z],1,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y+1,z],1,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y+1,z],1,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y+1,z],1,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopBack );      // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopBack );     // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopFor );       // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);           // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);           // Top right
              counter += 2;

              vertices.Enqueue( rightTopFor );      // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // --- Check forward ---
            if (z<chunk.blocks.GetLength(2)-1 && chunk.blocks[x,y,z+1] != (byte)BlockType.Air)
            {
              normal = -Vector3.forward;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z+1],5,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z+1],5,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z+1],5,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z+1],5,3];
            
              vertices.Enqueue( leftTopFor );       // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( rightTopFor );      // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( leftBotFor );       // Bottom left
              normals.Enqueue( normal ); 
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);


              // Second trianglenter++;
              triangles.Enqueue(counter);           // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);           // Top right
              counter += 2;

              vertices.Enqueue( rightBotFor );      // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter ++;
            }
          }
  // =============================================================================
  // === Render is next applied to solid blocks on the edge faces of the chunk ===
  // =============================================================================
          /*
          else
          {
            // Left face
            if (x==0)
            {
              normal = -Vector3.right;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z],2,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z],2,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z],2,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z],2,3];
              
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopBack );      // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotBack );     // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopFor );      // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);           // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);           // Top right
              counter += 2;

              vertices.Enqueue( leftBotFor );     // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Right face
            else if (x==chunk.blocks.GetLength(0)-1)
            {
              normal = Vector3.right;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z],3,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z],3,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z],3,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z],3,3];
              
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightTopFor );      // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotFor );     // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopBack );      // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);           // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);           // Top right
              counter += 2;

              vertices.Enqueue( rightBotBack );     // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Bottom face
            if (y==0)
            {
              normal = -Vector3.up;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z],1,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z],1,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z],1,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z],1,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightBotFor );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotFor );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotBack );     // Bottom Left
              normals.Enqueue( normal);
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( leftBotBack );    // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Top face
            if (y==chunk.blocks.GetLength(1)-1)
            {
              normal = Vector3.up;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z],0,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z],0,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z],0,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z],0,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopBack );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopFor );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopBack );     // Bottom Left
              normals.Enqueue( normal);
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( rightTopFor );    // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Back face
            if (z==0)
            {
              normal = -Vector3.forward;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z],5,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z],5,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z],5,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z],5,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightTopBack );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotBack );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopBack );     // Bottom Left
              normals.Enqueue( normal);
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( leftBotBack );    // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Front face
            if (z==chunk.blocks.GetLength(2)-1)
            {
              normal = Vector3.forward;
              uv1 = ChunkController.atlasUVs[chunk.blocks[x,y,z],4,0];
              uv2 = ChunkController.atlasUVs[chunk.blocks[x,y,z],4,1];
              uv3 = ChunkController.atlasUVs[chunk.blocks[x,y,z],4,2];
              uv4 = ChunkController.atlasUVs[chunk.blocks[x,y,z],4,3];

              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopFor );    // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv3 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotFor );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv2 );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopFor );     // Bottom Left
              normals.Enqueue( normal);
              uvs.Enqueue( uv4 );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);         // Bottom left
              counter -= 1;

              triangles.Enqueue(counter);         // Top right
              counter += 2;

              vertices.Enqueue( rightBotFor );    // Bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv1 );
              triangles.Enqueue(counter);
              counter ++;
            }
          }
          */
          

        }
      }
    }
    // End for loop
    /*
    Debug.Log("Vertex count: "+vertices.Count);
    Debug.Log("Triangle count: "+triangles.Count);
    Debug.Log("Normals count: "+normals.Count);
    Debug.Log("UV count: "+uvs.Count);
    */

    Mesh m = new Mesh();
    m.vertices = vertices.ToArray();
    m.triangles = triangles.ToArray();
    m.normals = normals.ToArray();
    m.uv = uvs.ToArray();

    myCollider.sharedMesh = m;
    myFilter.sharedMesh = m;
    myRend.sharedMaterial.mainTexture = ChunkController.textureAtlas;
  }
}
