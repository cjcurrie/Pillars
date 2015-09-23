using UnityEngine;
using System.Collections;

[System.Serializable]
public enum BlockType : byte {None,Air,Grass,Dirt,Water,Stone,Log,Leaves,Sand};

[System.Serializable]
public class BlockDefinition
{
  public BlockType type;
  public bool isDiggable, isBreakable, isTransparent;

  public Texture2D baseTexture;
  public TextureFaceMap[] specialFaceTextures;
  // 0-top 1-bottom 2-left 3-right 4-front 5-back
  public enum BlockFace{Top,Bottom,Left,Right,Front,Back};

  [System.Serializable]
  public class TextureFaceMap
  {
    public BlockFace face;
    public Texture2D texture;
  }

  public BlockDefinition()
  {
    isDiggable = false;
  }
}

public struct BlockAttribute
{
  public bool isDiggable, isBreakable, isTransparent;
  public BlockAttribute(bool dig, bool brea, bool transp)
  {
    isDiggable = dig;
    isBreakable = brea;
    isTransparent = transp;
  }
}

public static class Block {
  public static BlockAttribute[] attributes;
  public static void CreateAttributes(BlockDefinition[] input)
  {
    // Note that input is an unsorted array not suitable for indexing with BlockType
    attributes = new BlockAttribute[System.Enum.GetNames(typeof(BlockType)).Length];

    foreach (BlockDefinition d in input)
    {
      attributes[(int)d.type] = new BlockAttribute(d.isDiggable, d.isBreakable, d.isTransparent);
    }
  }
}
