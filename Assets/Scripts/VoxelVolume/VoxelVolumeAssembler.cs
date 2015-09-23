using UnityEngine;
using System.Collections;

using System.Collections.Generic;

public class VoxelVolumeAssembler : MonoBehaviour
{
  public UnityEngine.Object workingFile;
  public string pathToFile;

  public static Mesh MeshFromData( byte[] data_in, ushort width, ushort height, ushort[] dimensions_in, float voxelScale, int[] centerOffset)
  {
    //Vector3 offset1 = new Vector3( (float)dimensions_in[0], (float)dimensions_in[1], (float)dimensions_in[2]) * voxelScale/2f;
    Vector3 offset2 = new Vector3(centerOffset[0],centerOffset[1],centerOffset[2]) * voxelScale;
    
    Queue<Vector3> vertices = new Queue<Vector3>();
    Queue<int> triangles = new Queue<int>();
    Queue<Vector3> normals = new Queue<Vector3>();
    Queue<Vector2>uvs = new Queue<Vector2>();

    float uvIncr = (1f/(width-1f));   // Same as height
    //Debug.Log(uvIncr);
    float uvIncrY = (1f/(height-1f));

    int counter = 0;

    for (int x=0; x<dimensions_in[0]; x++)
    {
      for (int y=0; y<dimensions_in[1]; y++)
      {
        for (int z=0; z<dimensions_in[2]; z++)
        {
          int index = GetIndex(x,y,z, dimensions_in);
          byte color;

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

          Vector2 uv;
          Vector3 normal;
          
          //Debug.Log("x="+x+",y="+y+",z="+z+" gives index of "+index+" which contains color number "+data_in[index]);
          
          if (data_in[index] == 255)    // Empty space
          { 
            // --- Check left ---
            if (x>0)
            {
              color = data_in[GetIndex(x-1,y,z, dimensions_in)];
              if (color != 255)
              {
                normal = Vector3.right;
              
                uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);

                // --- Create front (rightmost) face of cube behind
                // First triangle 
                vertices.Enqueue( leftTopBack );   // top left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( leftTopFor );   // Top right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( leftBotBack );   // Bottom Left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
                triangles.Enqueue(counter);

                // Second triangle
                triangles.Enqueue(counter);   // bottom left
                counter -= 1;

                triangles.Enqueue(counter);   // top right
                counter += 2;

                vertices.Enqueue( leftBotFor );   // Bottom right
                normals.Enqueue( normal ); // Normal
                uvs.Enqueue( uv );// new Vector2((float)(uv.x+uvIncr), uv.y) );
                triangles.Enqueue(counter);
                counter ++;
              }
            }

            // --- Check down ---
            if (y>0)
            {
              color = data_in[GetIndex(x,y-1,z, dimensions_in)];
              //Debug.Log("x="+x+",y="+y+",z="+z+" yields "+color);
              if (color != 255)
              {
                normal = Vector3.up;
                uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);

                // --- Create front (rightmost) face of cube behind
                // First triangle 
                vertices.Enqueue( rightBotFor );   // top left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( rightBotBack );   // Top right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( leftBotFor );   // Bottom Left
                normals.Enqueue( normal);
                uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
                triangles.Enqueue(counter);

                // Second triangle
                triangles.Enqueue(counter);   // bottom left
                counter -= 1;

                triangles.Enqueue(counter);   // top right
                counter += 2;

                vertices.Enqueue( leftBotBack );   // Bottom right
                normals.Enqueue( normal ); // Normal
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
                triangles.Enqueue(counter);
                counter ++;
              }
            }

            // --- Check backward ---
            if (z>0)
            {
              color = data_in[GetIndex(x,y,z-1, dimensions_in)];
              //Debug.Log("x="+x+",y="+y+",z="+z+" yields "+color);
              if (color != 255)
              {
                normal = Vector3.forward;
                //Debug.Log(z);
                uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);
              
                vertices.Enqueue( rightTopBack );   // Top left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter ++;

                vertices.Enqueue( leftTopBack );   // Top right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter ++;

                vertices.Enqueue( rightBotBack );   // Bottom left
                normals.Enqueue( normal ); // Normal
                uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
                triangles.Enqueue(counter);

                // Second trianglenter++;
                triangles.Enqueue(counter);   // bottom left
                counter -= 1;

                triangles.Enqueue(counter);   // top right
                counter += 2;

                vertices.Enqueue( leftBotBack );   // bottom right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
                triangles.Enqueue(counter);
                counter ++;
              }
            }

            // --- Check right ---
            if (x<dimensions_in[0]-1)
            {
              color = data_in[GetIndex(x+1,y,z, dimensions_in)];
              if (color != 255)
              {
                normal = -Vector3.right;
              
                uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);
                //Debug.Log("Color: "+color+"  uv.x: "+uv.x+"  uvIncr: "+uvIncr+"  uv.y: "+uv.y+"  uvIncrY: "+uvIncrY);
              
                // --- Create front (rightmost) face of cube behind
                // First triangle 
                vertices.Enqueue( rightTopFor );   // top left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//+uvIncrY ) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( rightTopBack );   // Top right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//+uvIncr, uv.y+uvIncrY) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( rightBotFor );   // Bottom Left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );
                triangles.Enqueue(counter);

                // Second triangle
                triangles.Enqueue(counter);   // bottom left
                counter -= 1;

                triangles.Enqueue(counter);   // top right
                counter += 2;

                vertices.Enqueue( rightBotBack );   // Bottom right
                normals.Enqueue( normal ); // Normal
                uvs.Enqueue( uv );//+uvIncr, uv.y) );
                triangles.Enqueue(counter);
                counter ++;
              }
            }

            // --- Check up ---
            if (y<(int)dimensions_in[1]-1)
            {
              color = data_in[GetIndex(x,y+1,z, dimensions_in)];
              if (color != 255)
              {
                normal = -Vector3.up;
              
                uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);

                // --- Create front (rightmost) face of cube behind
                // First triangle 
                vertices.Enqueue( leftTopBack );   // top left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( rightTopBack );   // Top right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter++;

                vertices.Enqueue( leftTopFor );   // Bottom Left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );// new Vector2(uv.x, uv.y) );
                triangles.Enqueue(counter);

                // Second triangle
                triangles.Enqueue(counter);   // bottom left
                counter -= 1;

                triangles.Enqueue(counter);   // top right
                counter += 2;

                vertices.Enqueue( rightTopFor );   // Bottom right
                normals.Enqueue( normal ); // Normal
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
                triangles.Enqueue(counter);
                counter ++;
              }
            }

            // --- Check forward ---
            if (z<(int)dimensions_in[2]-1)
            {
              color = data_in[GetIndex(x,y,z+1, dimensions_in)];
              if (color != 255)
              {
                normal = -Vector3.forward;
              
                uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);
              
                vertices.Enqueue( leftTopFor );   // Top left
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter ++;

                vertices.Enqueue( rightTopFor );   // Top right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
                triangles.Enqueue(counter);
                counter ++;

                vertices.Enqueue( leftBotFor );   // Bottom left
                normals.Enqueue( normal ); // Normal
                uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
                triangles.Enqueue(counter);


                // Second trianglenter++;
                triangles.Enqueue(counter);   // bottom left
                counter -= 1;

                triangles.Enqueue(counter);   // top right
                counter += 2;

                vertices.Enqueue( rightBotFor );   // bottom right
                normals.Enqueue( normal );
                uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
                triangles.Enqueue(counter);
                counter ++;
              }
            }

            
          }

          // === Make the Top, bottom, right, and left edge sides
          else
          {
            color = data_in[GetIndex(x,y,z, dimensions_in)];
            if (color == 255)   // No color??
            {
              Debug.LogError("The color at data x,y,z={"+x+","+y+","+z+"} is null.");
              continue;
            }
            
            uv = GetUVFromColor(color, width, height, uvIncr, uvIncrY);
            
            // Right side
            if (z==0)
            { 
              normal = -Vector3.forward;      
              
              vertices.Enqueue( rightTopBack );   // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( rightBotBack );   // Bottom left
              normals.Enqueue( normal ); // Normal
              uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( leftTopBack );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);

              // Second trianglenter++;
              triangles.Enqueue(counter);   // top right
              counter -= 1;

              triangles.Enqueue(counter);   // bottom left
              counter += 2;

              vertices.Enqueue( leftBotBack );   // bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Left side
            if (z==(int)dimensions_in[2]-1)
            {
              normal = Vector3.forward;
              
              vertices.Enqueue( leftTopFor );   // Top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( leftBotFor );   // Bottom left
              normals.Enqueue( normal ); // Normal
              uvs.Enqueue( uv );// new Vector2(uv.x, uv.y) );
              triangles.Enqueue(counter);
              counter ++;

              vertices.Enqueue( rightTopFor );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);

              // Second trianglenter++;
              triangles.Enqueue(counter);   // top right
              counter -= 1;

              triangles.Enqueue(counter);   // bottom left
              counter += 2;

              vertices.Enqueue( rightBotFor );   // bottom right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Bottom side
            if (y==0)
            {
              normal = -Vector3.up;
               
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightBotFor );   // top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotFor );   // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotBack );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );// new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);   // bottom left
              counter -= 1;

              triangles.Enqueue(counter);   // top right
              counter += 2;

              vertices.Enqueue( leftBotBack );   // Bottom right
              normals.Enqueue( normal ); // Normal
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Top side
            if (y==(int)dimensions_in[1]-1)
            {
              normal = Vector3.up;
              
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopBack );   // top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopFor );   // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopBack );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);   // bottom left
              counter -= 1;

              triangles.Enqueue(counter);   // top right
              counter += 2;

              vertices.Enqueue( rightTopFor );   // Bottom right
              normals.Enqueue( normal ); // Normal
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
              triangles.Enqueue(counter);
              counter ++;
          }

            // Back side
            if (x==0)
            {
              normal = -Vector3.right;
              
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( leftTopBack );   // top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftBotBack );   // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( leftTopFor );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);   // bottom left
              counter -= 1;

              triangles.Enqueue(counter);   // top right
              counter += 2;

              vertices.Enqueue( leftBotFor );   // Bottom right
              normals.Enqueue( normal ); // Normal
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), uv.y) );
              triangles.Enqueue(counter);
              counter ++;
            }

            // Front side
            if (x==(int)dimensions_in[0]-1)
            {
              normal = Vector3.right;
              
              // --- Create front (rightmost) face of cube behind
              // First triangle 
              vertices.Enqueue( rightTopFor );   // top left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightBotFor );   // Bottom Left
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2(uv.x, uv.y) );
              triangles.Enqueue(counter);
              counter++;

              vertices.Enqueue( rightTopBack );   // Top right
              normals.Enqueue( normal );
              uvs.Enqueue( uv );//new Vector2((float)(uv.x+uvIncr), (float)(uv.y+uvIncrY)) );
              triangles.Enqueue(counter);

              // Second triangle
              triangles.Enqueue(counter);   // bottom left
              counter -= 1;

              triangles.Enqueue(counter);   // top right
              counter += 2;

              vertices.Enqueue( rightBotBack );   // Bottom right
              normals.Enqueue( normal ); // Normal
              uvs.Enqueue( uv );// new Vector2((float)(uv.x+uvIncr), uv.y) );
              triangles.Enqueue(counter);
              counter ++;
            }
          }
          
        }
      }
    }
    

    Mesh m = new Mesh();
    m.vertices = vertices.ToArray();
    m.triangles = triangles.ToArray();
    m.normals = normals.ToArray();
    m.uv = uvs.ToArray();

    return m;
  }

  public static int GetIndex( VoxelVolumeCreator obj, Vector3 pos, bool convertCoordsToLocalSpace )
  {
    if (convertCoordsToLocalSpace)
      pos = obj.myTrans.InverseTransformPoint(pos);
      
    // Add offset
    pos += new Vector3(obj.centerOffset[0], obj.centerOffset[1], obj.centerOffset[2]) * obj.voxelScale;
    
    int x = (int)Mathf.Floor( pos.x/obj.voxelScale );
    int y = (int)Mathf.Floor( pos.y/obj.voxelScale );
    int z = (int)Mathf.Floor( pos.z/obj.voxelScale );
    //Debug.Log("Adding voxel at data coords x="+x+", y="+y+", z="+z);
    return GetIndex( x,y,z, obj.dimensions );
  }
  
  public static int GetIndex( int x, int y, int z, ushort[] dimensions )
  { return (int)(x + y*dimensions[0] + z*dimensions[0]*dimensions[1]); }
    
  public static Vector3 GetOffset( int index, ushort[] dimensions_in )
  {
    return new Vector3( index%dimensions_in[0], (index/dimensions_in[0]) % dimensions_in[1], index/(dimensions_in[0]*dimensions_in[1]) );
  }


  static Vector2 GetUVFromColor (byte color, ushort width, ushort height, float uvIncr, float uvIncrY)
  {
    ushort colorHeight = height;
    if (width>height)
      colorHeight = width;
    int x = (color%width);
    float offset = .0001f;
    if ((x+1)>(width)/2)
      offset = -.0001f;
    
    return new Vector2( x*uvIncr + offset, (int)((color)/colorHeight)*uvIncrY + .0001f);
  }
}
