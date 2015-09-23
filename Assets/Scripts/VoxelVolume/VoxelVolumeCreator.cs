#define VC_DEBUG

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class VoxelVolumeCreator : MonoBehaviour {
  
  public bool isPrefab;    // If this is a prefab, the Editor class should chill out and not do anything.
  
  public Transform myTrans;
  public GameObject myGameObject;
  public Renderer myRend;
  MeshFilter meshFilter;
  MeshCollider meshCollider;
  
  public string lastSavedFileName;
  public string lastSavedFilePath;
  public string lastSavedGameObjectName;
  public string lastBackupPath;
  public const string voxelDataDirectory = "/Resources/VoxelData";
  public const string vDataBackupDirectory = "/Backups";
  public const string vDataExtension = "bytes";

  public bool autosaveOnFocusChange;
  
  public ushort[] dimensions = new ushort[3];
  public byte[] data;
  public Texture2D texture;
  public List<Color> colorPalette;
  public byte selectedColor;
  public float voxelScale = 1f;
  
  public float filterLevel = .07f;   // The opacity beneath which pixels are omitted when building from an image
  
  public int[] centerOffset;    // The vector that, when added to each polygon, moves the mesh to the default pos with transform pivot at the back-bottom-left
  
  public bool[] selectedList;
  
  public bool initialized {get; private set;}
  
  void Awake()
  {
    Destroy (gameObject);
  }
  
  public void Initialize()
  {
    #if VCE_DEBUG_On
      Debug.Log( "VC: Initializing VoxelVolumeCreator...." );
    #endif
    
    myTrans = transform;
    myGameObject = gameObject;
    myRend = GetComponent<Renderer>();
    meshFilter = (MeshFilter)GetComponent(typeof(MeshFilter));
    meshCollider = (MeshCollider)GetComponent(typeof(MeshCollider));
    
    bool fail = false;
    string err = "Initialize error on "+gameObject.name+": ";

    // === Failure conditions ===
    if (myRend.sharedMaterial == null)
    {
      err += "No Material found on Renderer. ";
    }
    else
      initialized = true;

    if(!Directory.Exists(Application.dataPath+voxelDataDirectory))
    {
      fail = true;
      err += "Directory "+Application.dataPath+voxelDataDirectory+" not found. Creating.... ";
      Directory.CreateDirectory(Application.dataPath+voxelDataDirectory);
    }

    if(!Directory.Exists(Application.dataPath+voxelDataDirectory+vDataBackupDirectory))
    {
      fail = true;
      err += "Directory "+Application.dataPath+voxelDataDirectory+vDataBackupDirectory+" not found. Creating.... ";
      Directory.CreateDirectory(Application.dataPath+voxelDataDirectory+vDataBackupDirectory);
    }

    if (fail)
      Debug.LogWarning(err);
  }
  
  public void Reset()
  {
    Reset(1,1,1);
  }
  public void Reset(ushort width, ushort height, ushort length)
  {    
    autosaveOnFocusChange = true;
    
    colorPalette = new List<Color>();
    colorPalette.Add(Color.white);
    selectedColor = 0;
    
    dimensions = new ushort[3];
    dimensions[0] = width; dimensions[1] = height; dimensions[2] = length;
    
    data = new byte[dimensions[0]*dimensions[1]*dimensions[2]];
    selectedList = new bool[dimensions[0]*dimensions[1]*dimensions[2]];
    centerOffset = new int[]{0,0,0};
    //voxelScale = 1f;
  }
  
  public void ClearSelectedList()
  {
    for (int i=0; i<selectedList.Length; i++)
      selectedList[i] = false;
  }
  
  public void BuildMesh()
  {
    if (gameObject == null)
    {
      Debug.LogError("Object was erased???");
      return;
    }
    
    if (!initialized)
      Initialize();
      
    Mesh m = VoxelVolumeAssembler.MeshFromData( data, (ushort)(texture.width), (ushort)(texture.height), dimensions, voxelScale, centerOffset );
    
    // Clean up old mesh
    if (!(meshFilter.sharedMesh == null))
      DestroyImmediate(meshFilter.sharedMesh);
    
    meshCollider.sharedMesh = m;
    meshFilter.sharedMesh = m; 
    
    // Clean up old material
    if (!(myRend.sharedMaterial == null))
    {
      if (!(myRend.sharedMaterial.mainTexture == null))
        DestroyImmediate(myRend.sharedMaterial.mainTexture);

      myRend.sharedMaterial.mainTexture = texture;
    }
    else
    {
      Debug.LogError("Is the VoxelVolumeCreator "+gameObject.name+" missing a Material?");
    }
  }
  
    
  public static Texture2D RebuildTexture( Color[] colors_in )
  {
    int textureWidth = (byte)Mathf.Ceil(Mathf.Sqrt((float)colors_in.Length));
    int textureHeight = (byte)colors_in.Length/textureWidth;
    
    if (textureWidth*textureHeight < colors_in.Length)
      textureHeight++;
        
    Color[] tmp = new Color[textureWidth*textureHeight];    // For the edge case: no trailing empty slots
    
    for (int i=0; i<tmp.Length; i++)
    {
      if (i<colors_in.Length)
        tmp[i] = colors_in[i];
      else
        tmp[i] = Color.clear;
    }
    
    Texture2D tex = new Texture2D( textureWidth, textureHeight, TextureFormat.RGB24, false );    // false = No mipmaps
    tex.filterMode = FilterMode.Point;
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.anisoLevel = 0;
    
    tex.SetPixels( tmp );
    
    tex.Apply();
    
    return tex;
  }
    
  
  // ========================================
  //  VolumeCreator save and load operations
  // ========================================
  // Note that no distinction is made here between save-to-file and autosave/save-to-backup.
  //  All changes to this object's filePath and backupPath are made in the VolumeObjectEditor
  //  filePath must be fully-qualified
  public bool DoSaveToDisk( string filePath )
  { 
    if (filePath =="")
    {
      Debug.LogError("No path provided to save funciton.");
      return false;
    }

    #if VC_DEBUG
      Debug.Log("VC: Writing file to "+filePath);
    #endif

    Color[] pixels = texture.GetPixels();
    float[,] tmp_colors = new float[pixels.Length, 4];
    for (uint i=0; i<pixels.Length; i++)
    {
      tmp_colors[i,0] = pixels[i].r;
      tmp_colors[i,1] = pixels[i].g;
      tmp_colors[i,2] = pixels[i].b;
      tmp_colors[i,3] = pixels[i].a;
    }
    
    VoxelVolumeData volData = new VoxelVolumeData( this.data, tmp_colors, this.dimensions, this.voxelScale, this.centerOffset );
    
    try
    {
      VoxelVolumeBinaryHandler.WriteToDisk(filePath, volData);
    }
    catch (System.Exception e)
    {
      Debug.LogError(e);
      return false;
    }

    return true;
  }
  
  public bool DoLoadFromFile( string path )
  {   
    #if VC_DEBUG
      Debug.Log("VC: Loading file at "+path);
    #endif

    VoxelVolumeData vvd;

    try
    {
      vvd = VoxelVolumeBinaryHandler.ReadFromDisk(path);
      this.data = vvd.data;
    }
    catch(System.Exception e)
    {
      Debug.LogError(e);
      return false;
    }
    
    // colors
    Color[] colors = new Color[vvd.colors.GetLength(0)];
    for (int i=0; i<colors.Length; i++)
      colors[i] = new Color(vvd.colors[i,0], vvd.colors[i,1], vvd.colors[i,2], vvd.colors[i,3]);
      
    this.colorPalette = new List<Color>();
    foreach (Color c in colors)
      this.colorPalette.Add(c);
      
    // dimensions
    this.dimensions = vvd.dimensions;
    
    // voxelScale
    this.voxelScale = vvd.voxelScale; 
    
    // centerOffset
    this.centerOffset = vvd.centerOffset;
    
    texture = RebuildTexture( colors );
    
    // Note that mesh-building and texturing is done by the VolumeCreatorEditor once this function completes.
    return true;
  }



  public string GetBackupPath()
  {
    return Application.dataPath + voxelDataDirectory + vDataBackupDirectory + "/" +gameObject.name + "." + vDataExtension;
  }

  public static string GetShortFileNameFromPath(string fullPath)
  {
    int index = fullPath.LastIndexOf('/');
    if (index < 0)
    {
      return fullPath;
    }
    int index2 = fullPath.LastIndexOf('/', index-1);
      
    return fullPath.Substring(index2+1, fullPath.Length-index2-1);
  }
}