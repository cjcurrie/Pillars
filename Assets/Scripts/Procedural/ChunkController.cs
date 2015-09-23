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

public class ChunkController : MonoBehaviour
{
// Cache
  public static Texture2D textureAtlas;
  public static Vector2[,,] atlasUVs;
  public static SimplexNoise simplex;
// Public/Inspector
  public int octaves=2, multiplier=25;
  public float amplitude=.5f, lacunarity=2;

  public GameObject grassPrefab,dirtPrefab,chunkPrefab;
  public int textureWidth = 128;
  public BlockDefinition[] blocksDefinition;

// Private
  static Dictionary<string, Chunk> allChunks;
  static List<Chunk> activeChunks;
  static PriorityQueue<float, Chunk> chunksToLoad;

  static int viewLengthInChunks;
  int[] playerPos;
  bool updatingChunks;


  public void Initialize(string seed)
  {
    Block.CreateAttributes(blocksDefinition);

    // Seed the generator
    simplex = new SimplexNoise(seed);

    // Init vars
    allChunks = new Dictionary<string, Chunk>();
    activeChunks = new List<Chunk>();
    chunksToLoad = new PriorityQueue<float, Chunk>(new ChunkDistanceComparer());

    // This is obviously not the player's real position, but it forces a chunk update when game starts
    playerPos = new int[]{999999999,999999999,999999999};

    InitializeTextures();
  }

  public void UpdateChunkController()
  {
    int[] newPos = GetPlayerPosInChunkCoords();

    // The player has crossed the border from one chunk to another.
    if (!(newPos[0]==playerPos[0] && newPos[1]==playerPos[1] && newPos[2]==playerPos[2]))
    {
      // @TODO: figure out vertical chunks so we can use a Y-value here
      StartCoroutine(UpdateChunkActivation(newPos));
      //QueueViewRangeChunks();
    }

    // Update chunk logic
    foreach(Chunk c in activeChunks){
      c.chunkRenderer.AutomaticRenderUpdate();
    }
  }

  void QueueViewRangeChunks()
  {
    // @TODO: do something with (PriorityQueue<T>)chunksToLoad
  }

  public IEnumerator UpdateChunkActivation(int[] newPos)
  {
    if (updatingChunks)
      yield break;

    updatingChunks = true;    // Locks this method

    List<int[]> chunkIndices = GetSurroundingChunkIndices(new int[]{newPos[0],0,newPos[2]});
    List<Chunk> oldList = new List<Chunk>(activeChunks);

    yield return StartCoroutine(ActivateChunks(chunkIndices));
    DisableInactiveChunks(oldList);

    playerPos = newPos;

    updatingChunks = false;
  }

  // Accesses the chunks from allChunks and assigns them into activeChunks
  //  OR if not in allChunks already, generates them, puts them in allChunks and activeChunks
  IEnumerator ActivateChunks(List<int[]> indices)
  {
    int worldHeight = 1 * Settings.CHUNK_SIZE;
    activeChunks = new List<Chunk>();

    foreach (int[] index in indices)
    {
      string accessor = Util.CoordsToString(index);
      Chunk c;

      // Chunk is currently loaded into allChunks
      if (allChunks.TryGetValue(accessor, out c))
      {
        activeChunks.Add(c);

        if (c.myObj == null)
          InstantiateChunk(c);
        if (!c.rendering)
        {
          c.rendering = true;
          c.chunkRenderer.Render();
        }
      }
      // Chunk is not found, so we create it
      else
      {
        c = new Chunk(octaves, multiplier, amplitude, lacunarity, index);
        Vector3 location = new Vector3(index[0],index[1],index[2])*Settings.CHUNK_SIZE;
        // Note that the location parameter unit is blocks, not chunks, nor meters
        c.Generate(location, worldHeight);
        
        InstantiateChunk(c);
        c.rendering = true;
        c.chunkRenderer.Render();

        allChunks[accessor] = c;
        activeChunks.Add(c);
      }

      yield return null;
    }
  }

  void InstantiateChunk(Chunk c)
  {
    Vector3 loc = new Vector3(c.coord[0],c.coord[1],c.coord[2]) * Settings.CHUNK_SIZE * Settings.BLOCK_SIZE;
    c.myObj = (GameObject)GameObject.Instantiate(chunkPrefab, loc, Quaternion.identity);
    c.myObj.name = "chunk "+"("+Util.CoordsToString(c.coord)+")";
    c.chunkRenderer = c.myObj.GetComponent<ChunkRenderer>();
    c.chunkRenderer.Initialize(c);
    c.myTrans = c.myObj.transform;
  }

  void DisableInactiveChunks(List<Chunk> oldList)
  {
    List<Chunk> toDisable = new List<Chunk>();

    foreach (Chunk c in oldList)
    {
      if (!activeChunks.Contains(c))
      {
        toDisable.Add(c);
      }
    }

    foreach(Chunk c in toDisable)
    {
      c.rendering = false;
      Destroy(c.myObj);
    }
  }

  IEnumerator RenderActiveChunks()
  {
    foreach (Chunk c in activeChunks)
    {
      if (c.rendering)
        continue;

      c.rendering = true;
      c.chunkRenderer.Render();
      yield return null;
    }
  }

  public bool CanPlaceBlock(BlockType type, int[] chunkCoord, int[] blockCoord)
  {
    // Check first to see if we are attempting one of the overlap blocks that techinically "belongs" to the next chunk over
    bool changed = false;
    if (blockCoord[0] == Settings.CHUNK_SIZE)   // x is actually in the next chunk
    {
      chunkCoord[0]++;
      blockCoord[0] = 0;
      changed = true;
    }
    if (blockCoord[1] == Settings.CHUNK_SIZE)
    {
      chunkCoord[1]++;
      blockCoord[1] = 0;
      changed = true;
    }
    if (blockCoord[2] == Settings.CHUNK_SIZE)
    {
      chunkCoord[2]++;
      blockCoord[2] = 0;
      changed = true;
    }

    if (changed)
      return CanPlaceBlock(type, chunkCoord, blockCoord);

    Chunk c;

    if (!allChunks.TryGetValue(Util.CoordsToString(chunkCoord), out c))
      return false;    // No chunk found

    return true;
  }
  public bool ReceiveBlockPlaced(int[] chunkCoord, int[] blockCoord, BlockType type)
  {
    Chunk c;

    if (!allChunks.TryGetValue(Util.CoordsToString(chunkCoord), out c))
      return false;    // No chunk found

    c.PlaceBlock(blockCoord, type);

    // This code block updates nearby chunks that may be caching the "seam" blocks
    if (blockCoord[0]==0)
    {
      Chunk d;
      int[] neighborCoord = new int[]{chunkCoord[0]-1, chunkCoord[1], chunkCoord[2]};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out d))
        d.PlaceBlock(new int[]{Settings.CHUNK_SIZE, blockCoord[1], blockCoord[2]}, type);
    }
    if (blockCoord[1]==0)
    {
      Chunk e;
      int[] neighborCoord = new int[]{chunkCoord[0], chunkCoord[1]-1, chunkCoord[2]};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out e))
        e.PlaceBlock(new int[]{blockCoord[0], Settings.CHUNK_SIZE, blockCoord[2]}, type);
    }
    if (blockCoord[2]==0)
    {
      Chunk f;
      int[] neighborCoord = new int[]{chunkCoord[0], chunkCoord[1], chunkCoord[2]-1};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out f))
        f.PlaceBlock(new int[]{blockCoord[0], blockCoord[1], Settings.CHUNK_SIZE}, type);
    }

    if (blockCoord[0]==0 && blockCoord[1] == 0 && blockCoord[2]==0)   // Update down-left-back
    {
      Chunk g;
      int[] neighborCoord = new int[]{chunkCoord[0]-1, chunkCoord[1]-1, chunkCoord[2]-1};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out g))
        g.PlaceBlock(new int[]{Settings.CHUNK_SIZE, Settings.CHUNK_SIZE, Settings.CHUNK_SIZE},type);
    }
    else if (blockCoord[0]==0 && blockCoord[2]==0)  // Update left-back
    {
      Chunk h;
      int[] neighborCoord = new int[]{chunkCoord[0]-1, chunkCoord[1], chunkCoord[2]-1};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out h))
        h.PlaceBlock(new int[]{Settings.CHUNK_SIZE, blockCoord[1], Settings.CHUNK_SIZE},type);
    }

    return true;
  }

  public BlockType CanDigBlock(int[] chunkCoord, int[] blockCoord)
  {
    bool changed = false;
    if (blockCoord[0] == Settings.CHUNK_SIZE)   // x is actually in the next chunk
    {
      chunkCoord[0]++;
      blockCoord[0] = 0;
      changed = true;
    }
    if (blockCoord[1] == Settings.CHUNK_SIZE)
    {
      chunkCoord[1]++;
      blockCoord[1] = 0;
      changed = true;
    }
    if (blockCoord[2] == Settings.CHUNK_SIZE)
    {
      chunkCoord[2]++;
      blockCoord[2] = 0;
      changed = true;
    }

    if (changed)
      return CanDigBlock(chunkCoord, blockCoord);

    Chunk c;

    if (!allChunks.TryGetValue(Util.CoordsToString(chunkCoord), out c))
      return BlockType.None;    // No chunk found

    BlockType output = (BlockType)c.blocks[blockCoord[0],blockCoord[1],blockCoord[2]];

    return output;
  }

  public bool ReceiveBlockDug(int[] chunkCoord, int[] blockCoord)
  {
    Chunk c;

    if (!allChunks.TryGetValue(Util.CoordsToString(chunkCoord), out c))
      return false;    // No chunk found

    c.DigBlock(blockCoord);

    // This code block updates nearby chunks that may be caching the "seam" blocks
    if (blockCoord[0]==0)   // Update left
    {
      Chunk d;
      int[] neighborCoord = new int[]{chunkCoord[0]-1, chunkCoord[1], chunkCoord[2]};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out d))
        d.DigBlock(new int[]{Settings.CHUNK_SIZE, blockCoord[1], blockCoord[2]});
    }
    if (blockCoord[1]==0)   // Update down
    {
      Chunk e;
      int[] neighborCoord = new int[]{chunkCoord[0], chunkCoord[1]-1, chunkCoord[2]};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out e))
        e.DigBlock(new int[]{blockCoord[0], Settings.CHUNK_SIZE, blockCoord[2]});
    }
    if (blockCoord[2]==0)   // Update back
    {
      Chunk f;
      int[] neighborCoord = new int[]{chunkCoord[0], chunkCoord[1], chunkCoord[2]-1};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out f))
        f.DigBlock(new int[]{blockCoord[0], blockCoord[1], Settings.CHUNK_SIZE});
    }

    if (blockCoord[0]==0 && blockCoord[1] == 0 && blockCoord[2]==0)   // Update down-left-back
    {
      Chunk g;
      int[] neighborCoord = new int[]{chunkCoord[0]-1, chunkCoord[1]-1, chunkCoord[2]-1};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out g))
        g.DigBlock(new int[]{Settings.CHUNK_SIZE, Settings.CHUNK_SIZE, Settings.CHUNK_SIZE});
    }
    else if (blockCoord[0]==0 && blockCoord[2]==0)  // Update left-back
    {
      Chunk h;
      int[] neighborCoord = new int[]{chunkCoord[0]-1, chunkCoord[1], chunkCoord[2]-1};
      if (allChunks.TryGetValue(Util.CoordsToString(neighborCoord), out h))
        h.DigBlock(new int[]{Settings.CHUNK_SIZE, blockCoord[1], Settings.CHUNK_SIZE});
    }

    return true;
  }

  static List<int[]> GetSurroundingChunkIndices(int[] playerPos)
  {
    viewLengthInChunks = 2*Settings.chunkRenderDistance-1;
    List<int[]> output = new List<int[]>();

    for (int i=0; i<viewLengthInChunks*viewLengthInChunks; i++)
    {
      int[] offset = SpiralAroundAnchor(i);
      output.Add( new int[]{playerPos[0]+offset[0], playerPos[1], playerPos[2]+offset[1]} );
      // Note we're currently only generating chunks at the same y-axis as player
    }

    return output;
  }

  static int[] GetPlayerPosInChunkCoords()
  {
    Vector3 pos = GameController.playerTrans.position / (Settings.CHUNK_SIZE*Settings.BLOCK_SIZE);
    return new int[]{(int)pos.x, (int)pos.y, (int)pos.z};
  }

  static int[] SpiralAroundAnchor(int i)
  {
    int n = i-1;

    if (n < 0)
      return new int[]{0,0};

    // given n an index in the squared spiral
    // p the sum of point in inner square
    // a the position on the current square
    // n = p + a

    int r = (int)(Mathf.Floor((Mathf.Sqrt(n + 1) - 1) / 2) + 1);

    // compute radius : inverse arithmetic sum of 8+16+24+...=
    int p = (8 * r * (r - 1)) / 2;
    // compute total point on radius -1 : arithmetic sum of 8+16+24+...

    int en = r * 2;
    // points by face

    int a = (1 + n - p) % (r * 8);
    // compute de position and shift it so the first is (-r,-r) but (-r+1,-r)
    // so square can connect

    int[] pos = new int[]{0, 0};
    switch ((int)(Mathf.Floor(a / (r * 2)))) {
        // find the face : 0 top, 1 right, 2, bottom, 3 left
        case 0:
            {
                pos[0] = a - r;
                pos[1] = -r;
            }
            break;
        case 1:
            {
                pos[0] = r;
                pos[1] = (a % en) - r;

            }
            break;
        case 2:
            {
                pos[0] = r - (a % en);
                pos[1] = r;
            }
            break;
        case 3:
            {
                pos[0] = -r;
                pos[1] = r - (a % en);
            }
            break;
    }
    return pos;
  }

  void InitializeTextures()
  {
    int numberOfTypes = Enum.GetNames(typeof(BlockType)).Length;
    Dictionary<int, Texture2D> textureIDMap = new Dictionary<int, Texture2D>();
    //Dictionary<int, BlockFacePair> blockfaceIDMap = new Dictionary<int, BlockFacePair>();
    int[,] blocktypeTextureMap = new int[numberOfTypes,6];

    foreach(BlockDefinition b in blocksDefinition)
    {
      foreach (BlockDefinition.TextureFaceMap t in b.specialFaceTextures)
      {
        // First, assign the face textures
        int id = t.texture.GetInstanceID();
        blocktypeTextureMap[(int)b.type,(int)t.face] = id;
        textureIDMap[id] = t.texture;
        //blockfaceIDMap[id] = new BlockFacePair(b.type, t.face);
      }

      // Second, assign the baseTexture for all unfilled faces
      int baseID = b.baseTexture.GetInstanceID();
      textureIDMap[baseID] = b.baseTexture;
      for (int i=0;i<6;i++)
      {
        if (blocktypeTextureMap[(int)b.type,i] == 0)
        {
          blocktypeTextureMap[(int)b.type,i] = baseID;
        }
      }
    }

    Texture2D[] sortedTextures = new Texture2D[textureIDMap.Values.Count];
    Dictionary<int, int> idToTextureIndexMap = new Dictionary<int,int>();

    int count = 0;
    foreach (KeyValuePair<int, Texture2D> pair in textureIDMap)
    {
      sortedTextures[count] = pair.Value;
      idToTextureIndexMap[pair.Key] = count;
      count++;
    }


    // @TODO: calculate maxWidth so there is no wasted texture memory
    int maxWidth = 1024;//(int)(Mathf.Ceil(Mathf.Sqrt(textures.Length)) * textureWidth);

    textureAtlas = new Texture2D(maxWidth, maxWidth);
    Rect[] uvs = textureAtlas.PackTextures(sortedTextures, 0, maxWidth);

    atlasUVs = new Vector2[numberOfTypes, 6, 4];
    // @TODO: find a better fix to the UV mapping than this kludgy padding workaround
    float padding = .001f;    // May not scale well with large textures

    for (int j=0; j<numberOfTypes; j++)
    {
      // Sides are defined as 
      for (int k=0;k<6;k++)
      {
        int textureInstanceID = blocktypeTextureMap[j,k];
        if (textureInstanceID == 0)
          continue;

        int l = idToTextureIndexMap[textureInstanceID];

        // Atlas UVs are defined 0=bottomLeft, 1=bottomRight, 2=topRight, 3=topLeft
        atlasUVs[j,k,0] = new Vector2(uvs[l].x+padding, uvs[l].y+padding);
        atlasUVs[j,k,1] = new Vector2(uvs[l].x+uvs[l].width-padding, uvs[l].y+padding);
        atlasUVs[j,k,2] = new Vector2(uvs[l].x+uvs[l].width-padding, uvs[l].y+uvs[l].height-padding);
        atlasUVs[j,k,3] = new Vector2(uvs[l].x+padding, uvs[l].y+uvs[l].height-padding);
      }
    }
  }
}