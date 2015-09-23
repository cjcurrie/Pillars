#define VCE_DEBUG

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// @TODO: Undo.SetSnapshotTarget

[CustomEditor(typeof(VoxelVolumeCreator))]
public class VoxelVolumeCreatorEditor : Editor
{
  VoxelVolumeCreator vol;
  GameObject volObject;

  Texture2D textureInputXY, textureInputZY;

  bool initialized;

  bool changesMade;

  public int expansionAmount = 5;

  static double lastTime;
  static Transform curCamTrans;
  static bool lightsOff;

  UnityEngine.Object newObject;
  UnityEngine.Object oldObject;
  int[] newOffset;

  bool selectingNewColorToPlace;
  Vector3 colorPickedPlaceToPutVoxel;
  Rect colorWheelAnchor;
  Color wheelPickedColor;

  Texture2D colorWheel, editorSplash;

  bool paintingVoxels;

  void Initialize()
  {
    // === Cache ===
    if (colorWheel == null) //lightTrans == null ||
    {
      #if VCE_DEBUG
        Debug.Log( "VCE: Initializing VoxelVolumeCreatorEditor cache...." );
      #endif

      InitializeCache();
    }

    // === Cache vol ===
    vol = (VoxelVolumeCreator)target;
    if (PrefabUtility.GetPrefabType(vol) == PrefabType.Prefab)
    {
      vol.isPrefab = true;
      return;
    }
    else
    {
      vol.isPrefab = false;
    }

    volObject = vol.gameObject;
    if (!vol.initialized)
    {
      vol.Initialize();
      vol.Reset();
    }

    #if VCE_DEBUG
    //Rect handleRect = new Rect(0,0,Screen.width, Screen.height);
    //Handles.DrawCamera(new Rect(0,0,Screen.width, Screen.height), handles.currentCamera);
    //DrawBounds();
    #endif

    initialized = true;

    EditorPrefs.SetString( "lastObj", vol.gameObject.name );
  }

  void InitializeCache()
  {
    newOffset = new int[3];
    colorWheel = (Texture2D)( AssetDatabase.LoadAssetAtPath("Assets/Editor/VoxelVolumeEditor/images/REQUIRED_colorWheel.png", typeof(Texture2D) ));
    editorSplash = (Texture2D)( AssetDatabase.LoadAssetAtPath("Assets/Editor/VoxelVolumeEditor/images/REQUIRED_editorSplash.jpg", typeof(Texture2D) ));

    if (colorWheel == null)
    {
      Debug.LogError("Initialize failed. The color wheel cannot be found in /Assets/Editor/VoxelVolumeEditor/images/REQUIRED_colorWheel.png \nThis image is required to color voxels.");
    }
  }

	public override void OnInspectorGUI ()
	{
		if (!initialized && !(vol==null) && !vol.isPrefab)
    {
		  Initialize();
    }

    GUILayout.Space(15);

    if (!(editorSplash == null))
    {
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
  		GUILayout.Label(editorSplash);
  		GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
		}

		GUILayout.Space(15);

    if (vol.myGameObject.layer != 31)
    {
      GUILayout.Space(5);
      GUILayout.Label( "WARNING: THIS OBJECT MUST BE PLACED IN LAYER 31 TO FUNCTION." );
      GUILayout.Space(5);
    }

		if (vol.isPrefab)
		{
		  GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
		  GUILayout.Label( "Drag this prefab into the scene\nto begin creating voxels." );
		  GUILayout.FlexibleSpace();
    	GUILayout.EndHorizontal();

		  return;
		}

		// === SAVE/LOAD ===
    GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      GUILayout.BeginVertical();
        if (GUILayout.Button("Save backup"))
          SaveBackup();
        else if (GUILayout.Button("Save As"))
          SaveAs();
      GUILayout.EndVertical();
      if (GUILayout.Button("Create a new\nVoxel Volume"))
        NewVolume();
      GUILayout.BeginVertical();
        if (GUILayout.Button("Load Backup"))
          LoadBackup();
        else if (GUILayout.Button("Load..."))
          LoadAs();
      GUILayout.EndVertical();

      GUILayout.FlexibleSpace();
  	GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (changesMade)
          GUILayout.Label("File is being edited.");
      else
        GUILayout.Label("File is saved.");
      GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

  	GUILayout.Space(25);


    // === CONTROLS ===


    if (GUILayout.Button ("Open Color Palette", GUILayout.Height(50)))
    {
      VoxelColorPaletteWindow.Initialize(vol, this);
    }

    float newVSize = EditorGUILayout.FloatField("Voxel Scale: ", vol.voxelScale);
		if (newVSize != vol.voxelScale)
		{
		  vol.voxelScale = newVSize;
		  changesMade = true;
		  BuildMeshAndTexture();
		}

		GUILayout.Label("Pivot offset from -x,-y,-z\ncorner of volume:");

		  newOffset[0] = EditorGUILayout.IntField("X:", vol.centerOffset[0]);
      newOffset[1] = EditorGUILayout.IntField("Y:", vol.centerOffset[1]);
      newOffset[2] = EditorGUILayout.IntField("Z:", vol.centerOffset[2]);

      //Debug.Log("vol.centerOffset[0]: "+vol.centerOffset[0]+"   newOffset[0]: "+newOffset[0]);

		  if (newOffset[0]!=vol.centerOffset[0] || newOffset[1]!=vol.centerOffset[1] || newOffset[2]!=vol.centerOffset[2])
  		{
  		  vol.centerOffset = newOffset;
  		  changesMade = true;
  		  BuildMeshAndTexture();
  		}

    if (GUILayout.Button("Rebuild Mesh/Re-paint from Palette"))
    {
      OnDisable();
      BuildMeshAndTexture();
    }


		GUILayout.Space( 15 );



  	// --- Build a single layer from an image
  	textureInputXY = (Texture2D)EditorGUILayout.ObjectField("Front view: ", textureInputXY, typeof(Texture2D), false );
  	textureInputZY = (Texture2D)EditorGUILayout.ObjectField("Side view: ", textureInputZY, typeof(Texture2D), false );

    if (GUILayout.Button("Generate Slice from Texture"))
    {
      //SaveAs(true);
      VolumeFromImage();
      BuildMeshAndTexture();
    }

    GUILayout.Space(15);


    // === General Options ===

    // Flip over right, up, for forward axis
    GUILayout.BeginHorizontal();
      GUILayout.Label("Flip data along axis:");
		  if (GUILayout.Button("X"))
		  {
		    FlipDataOverAxis( 0 );
		    BuildMeshAndTexture();
		  }
		  else if (GUILayout.Button("Y"))
		  {
  		  FlipDataOverAxis( 1 );
  		  BuildMeshAndTexture();
		  }
	    else if (GUILayout.Button("Z"))
	    {
  		  FlipDataOverAxis( 2 );
  		  BuildMeshAndTexture();
		  }
		  GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

	  // --- Use autosave option ---
    vol.autosaveOnFocusChange = GUILayout.Toggle(vol.autosaveOnFocusChange,"Autosave to backup on focus change?");
    //GUILayout.Label("(Current backup path is:\n"+vol.shortBackupPath+")");
	}

  public void OnSceneGUI()
  {
    if (!initialized)
		  Initialize();

    Event e = Event.current;


/*    if (selectingNewColorToPlace)
    {
      // Display color picker image
      Handles.BeginGUI();
        GUI.DrawTexture(colorWheelAnchor, colorWheel);
      Handles.EndGUI();

      Vector2 pickpos = Event.current.mousePosition;

       aaa = pickpos.x-xxx-9;
       bbb = pickpos.y-yyy-5;
       col = colorPicker.GetPixel(aaa,41-bbb);

      if (e.button == 0)    // Left click
      {
       selectingNewColorToPlace = false;

        int selectedIndex = vol.GetIndex( hit.point - hit.normal * vol.voxelScale/4f, true );   // true = do convert coords to local space
        AddVoxel( colorPickedPlaceToPutVoxel, //vol.data[selectedIndex]);
        BuildMeshAndTexture();
      }
    }*/

    if (selectingNewColorToPlace)
    {
      wheelPickedColor = EditorGUI.ColorField (colorWheelAnchor, wheelPickedColor);

      if (wheelPickedColor != Color.white)
      {
        #if VCE_DEBUG
          Debug.Log(wheelPickedColor);
        #endif

        vol.selectedColor = AddVoxel(colorPickedPlaceToPutVoxel, wheelPickedColor);

        selectingNewColorToPlace = false;
      }
    }

    if (e.isMouse)
    {
      if (e.type == EventType.MouseUp && e.button == 0)     // Left mouse button released
      {
        if (paintingVoxels)
        {
          paintingVoxels = false;
        }
      }

      RaycastHit hit;
      Ray r = HandleUtility.GUIPointToWorldRay( e.mousePosition );

      // === Check for mesh interaction ===
      if (Physics.Raycast( r, out hit, 100, (1<<31) ))   // Everything but ignore raycast
      {
        // === Deselect and return if something else was clicked ===
        if (hit.transform != vol.myTrans)
        {
          return;
        }


        // === Display pointerCube ===
        Vector3 v = vol.myTrans.InverseTransformPoint( (hit.point + hit.normal*vol.voxelScale/4f) );

        //v = new Vector3( Mathf.Round((v.x/vol.voxelScale)), Mathf.Round((v.y/vol.voxelScale)), Mathf.Round((v.z/vol.voxelScale)) )*vol.voxelScale;
        v = new Vector3( (int)Mathf.Floor(v.x/vol.voxelScale), (int)Mathf.Floor(v.y/vol.voxelScale), (int)Mathf.Floor(v.z/vol.voxelScale) )*vol.voxelScale + (Vector3.one*vol.voxelScale/2f);
        v = vol.myTrans.TransformPoint(v);
                            //Debug.Log(vol.selectedColor+" "+vol.colorPalette.Count);
        Handles.color = new Color(vol.colorPalette[vol.selectedColor].r, vol.colorPalette[vol.selectedColor].g, vol.colorPalette[vol.selectedColor].b, .25f);
        //Handles.color = vol.colorPalette[vol.selectedColor];
        Handles.CubeCap(0, v, vol.myTrans.rotation, vol.voxelScale);



        // === Interact with VolumeObject vol data ===
        if (e.type == EventType.MouseDown && !(
            e.modifiers == EventModifiers.Shift ||
            e.modifiers == EventModifiers.Control ||
            e.modifiers == EventModifiers.Alt ||
            e.modifiers == EventModifiers.Command))
        {
          // --- Left click ---
          if (e.button == 0)    // Left click
          {
            // --- shift+LMB to add a tile to the selection
            if (e.shift)
            {
              int i = VoxelVolumeAssembler.GetIndex(vol, hit.point - hit.normal*vol.voxelScale/2f, true);
              AddVoxel(hit.point + hit.normal*vol.voxelScale/2f, vol.data[i]);
              //vol.selectedList[i] = true;
              BuildMeshAndTexture();
            }

            // --- ctrl+LMB to open color picker to create a new block with a new color
            else if (e.control)
            {
              //Debug.Log("adding a color via wheel");
              colorPickedPlaceToPutVoxel = hit.point + hit.normal*vol.voxelScale/4f;
              colorWheelAnchor = new Rect(Event.current.mousePosition.x-20, Event.current.mousePosition.y-20, 50,50);
              wheelPickedColor = Color.white;

              selectingNewColorToPlace = true;
            }

            // --- LMB for place block of selected color
            else
            {
              paintingVoxels = true;
              //int i = vol.GetIndex(hit.point + hit.normal*vol.voxelScale/4f, true);
              AddVoxel(hit.point + hit.normal*vol.voxelScale/4f, vol.selectedColor);   // byte selectedColor
              BuildMeshAndTexture();
            }
          }

          // --- Right click ---
          else if (e.button == 1)
          {
            // --- shift+RMB
            if (e.shift)
            {

            }

            // --- RMB to remove block
            else
            {
              int index = VoxelVolumeAssembler.GetIndex( vol, hit.point - hit.normal * vol.voxelScale/4f, true );

              RemoveVoxel( index );
              BuildMeshAndTexture();
            }
          }

          // --- Middle Mouse click to Select the color of the block---
          else if (e.button == 2)
          {
            vol.selectedColor = vol.data[VoxelVolumeAssembler.GetIndex( vol, hit.point - hit.normal * vol.voxelScale/4f, true)];
          }

          //DrawBounds();
        }


/*        if (paintingVoxels && e.type == EventType.MouseDrag)
        {
          int i = vol.GetIndex(hit.point + hit.normal*vol.voxelScale/4f, true);

          if (i != lastPaintedVoxel)
          {
            lastPaintedVoxel = i;
            AddVoxel(i, vol.selectedColor);
            BuildMeshAndTexture();
          }
        }*/

      }
    }

    Selection.activeGameObject = volObject;

    /*
    else if (e.isKey && e.type==EventType.KeyUp)   // Is a keyboard event
    {
      switch (e.character)
      {
        case '0': vol.selectedColor = 0;
          if (vol.selectedColor > vol.colorPalette.Count-1)
            vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '1': vol.selectedColor = 1;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '2': vol.selectedColor = 2;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '3': vol.selectedColor = 3;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '4': vol.selectedColor = 4;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '5': vol.selectedColor = 5;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '6': vol.selectedColor = 6;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '7': vol.selectedColor = 7;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '8': vol.selectedColor = 8;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
        case '9': vol.selectedColor = 9;
          if (vol.selectedColor > vol.colorPalette.Count-1)
          vol.selectedColor = (byte)(vol.colorPalette.Count-1);
        break;
      }
    }
    */

    SceneView.RepaintAll();
  }

  // void DragFileField()
  // {
  //   GUILayout.Label("Drag a .bytes file here to load it");
	 //  newObject = EditorGUILayout.ObjectField(newObject, typeof(UnityEngine.Object), false);    // No scene objects
	 //  if (!(newObject == null) && (!(oldObject == null) && newObject.name != oldObject.name) || oldObject == null)
	 //  {
	 //    oldObject = newObject;
	 //    string s = AssetDatabase.GetAssetPath(newObject);
	 //    if (s == "")
	 //      return;
	 //    vol.lastSavedFilePath = s;
	 //    //vol.shortFilePath = VoxelVolumeCreator.GetShortFileNameFromPath(s);
	 //    LoadFromFile(s);
  //   }
  // }

  #if VCE_DEBUG
  void DrawBounds()
  {

    Vector3 o = new Vector3(vol.centerOffset[0], vol.centerOffset[1], vol.centerOffset[2]) * vol.voxelScale * -1;
    Vector3 dim = new Vector3(vol.dimensions[0], vol.dimensions[1], vol.dimensions[2]) * vol.voxelScale;

    Vector3 backLeftDown, backRightDown, forLeftDown, forRightDown, backLeftUp, backRightUp, forLeftUp, forRightUp;

    backLeftDown = vol.myTrans.TransformPoint(o);
    backRightDown = vol.myTrans.TransformPoint(new Vector3(dim.x, 0, 0)+o);
    forLeftDown = vol.myTrans.TransformPoint(new Vector3(0, 0, dim.z)+o);
    forRightDown = vol.myTrans.TransformPoint(new Vector3(dim.x, 0, dim.z)+o);

    backLeftUp = vol.myTrans.TransformPoint(new Vector3(0, dim.y, 0)+o);
    backRightUp = vol.myTrans.TransformPoint(new Vector3(dim.x, dim.y, 0)+o);
    forLeftUp = vol.myTrans.TransformPoint(new Vector3(0, dim.y, dim.z)+o);
    forRightUp = vol.myTrans.TransformPoint(dim+o);

    Debug.Log(o);

    // Draw the 12 lines
    Handles.DrawLine (backLeftDown, backRightDown);
    Handles.DrawLine (backLeftDown, forLeftDown);
    Handles.DrawLine (backLeftDown, backLeftUp);
    Handles.DrawLine (backRightDown, backRightUp);
    Handles.DrawLine (backRightDown, forRightDown);
    Handles.DrawLine (backLeftUp, backRightUp);
    Handles.DrawLine (backLeftUp, forLeftUp);
    Handles.DrawLine (backRightUp, forRightUp);
    Handles.DrawLine (forLeftDown, forRightDown);
    Handles.DrawLine (forLeftUp, forRightUp);
    Handles.DrawLine (forLeftDown, forLeftUp);
    Handles.DrawLine (forRightDown, forRightUp);
  }
  #endif

	bool WithinDimensionalBounds( Vector3 p, Vector3 max, Vector3 min )
	{
	  // This assumes that p is in local coords
	  if (p.x>=min.x && p.x<=max.x
	    && p.y>=min.y && p.y<=max.y
	    && p.z>=min.z && p.z<=max.z)
	    return true;
	  else
	  {
  		return false;
		}
	}

	void TrimDimensions()
	{
	  if (vol.data.Length < 2)
	    return;

		int minX = -1, maxX = 0, minY = -1, maxY = 0, minZ = -1, maxZ = 0;

		for (ushort x=0; x<vol.dimensions[0]; x++)
		{
		  for (ushort y=0; y<vol.dimensions[1]; y++)
		  {
		    for (ushort z=0; z<vol.dimensions[2]; z++)
		    {
		      int index = VoxelVolumeAssembler.GetIndex(x,y,z, vol.dimensions);
		      if (vol.data[index] == 255)
		        continue;

		      // We found a block. Do compares


		      // Found a min
		      if (minX == -1)
		        minX = x;
		      else if (x<minX)
		        minX = x;
		      if (minY == -1)
		        minY = y;
		      else if (y<minY)
		        minY = y;
	        if (minZ == -1)
		        minZ = z;
		      else if (z < minZ)
		        minZ = z;

		      // Found a max
		      if (x > maxX)
		        maxX = x;
		      if (y > maxY)
		        maxY = y;
	        if (z > maxZ)
		        maxZ = z;
		    }
		  }
		}

		//Debug.Log("minX: "+minX+" maxX: "+maxX + "    minY: "+minY+" maxY: "+maxY + "   minZ: "+minZ+" maxZ: "+maxZ );
		ushort[] newDimensions = new ushort[3];
		newDimensions[0] = (ushort)(maxX - minX + 1);
		newDimensions[1] = (ushort)(maxY - minY + 1);
		newDimensions[2] = (ushort)(maxZ - minZ + 1);
		byte[] newData = new byte[ newDimensions[0] * newDimensions[1] * newDimensions[2] ];


		for (int x=minX; x<=maxX; x++)
		{
		  for (int y=minY; y<=maxY; y++)
		  {
		    for (int z=minZ; z<=maxZ; z++)
		    {
		      int index = VoxelVolumeAssembler.GetIndex(x,y,z, vol.dimensions);
		      int newIndex = (int)((x-minX) + (y-minY)*newDimensions[0] + (z-minZ)*newDimensions[0]*newDimensions[1]);
		      //Debug.Log("vol.data.Length: "+vol.data.Length+"   index: "+index+"   newData.Length: "+newData.Length+"   newIndex: "+newIndex);
		      newData[newIndex] = vol.data[index];
		    }
		  }
		}

    vol.centerOffset = new int[3]{vol.centerOffset[0]-minX, vol.centerOffset[1]-minY, vol.centerOffset[2]-minZ};
		vol.dimensions = newDimensions;
		vol.data = newData;
	}

	void RemoveVoxel( int index )
	{
    try 
    {
      vol.data[index] = 255;    // null case
      // @TODO: remove color from palette?
    }
	  catch (System.SystemException e)
    {
      Debug.LogError ("Bad attempt to access voxel "+index+"in volume of length "+vol.data.Length+"  "+e);
    }
	}

  void NewVolume()
  {
    vol.Reset();
    Initialize();

    VoxelColorPaletteWindow.Initialize(vol, this);

	  vol.dimensions = new ushort[3];
	  vol.dimensions[0] = 1; vol.dimensions[1] = 1; vol.dimensions[2] = 1;

    vol.data = new byte[ vol.dimensions[0] * vol.dimensions[1] * vol.dimensions[2] ];

    for (int i=0; i<vol.data.Length; i++)
      vol.data[i] = 255;    // The null case

    AddVoxel( VoxelVolumeAssembler.GetIndex(vol.dimensions[0]/2, vol.dimensions[1]/2, vol.dimensions[2]/2, vol.dimensions), 0);   // Starting point

    BuildMeshAndTexture();
  }

  // --- Neither index nor colorIndex are known ---
  byte AddVoxel( Vector3 point, Color color )     // Returns colorIndex
  {
   byte colorIndex = AddColor( color );   // Add the new color
   AddVoxel( point, colorIndex );   // Calls the function below

   return colorIndex;
  }

  // --- Only the colorIndex is known ---
  int AddVoxel( Vector3 pointInWorld, byte colorIndex)     // returns index
  {
    int index = -1;
    Vector3 offset = new Vector3(vol.centerOffset[0], vol.centerOffset[1], vol.centerOffset[2]) * vol.voxelScale;
    Vector3 point = vol.myTrans.InverseTransformPoint(pointInWorld);    // Turns the raycastHit.point into a local point
    Vector3 p = point+offset;
    Vector3 max = new Vector3(vol.dimensions[0], vol.dimensions[1], vol.dimensions[2]) * vol.voxelScale;

    // We need to expand
  	if (!WithinDimensionalBounds(p, max, Vector3.zero))
  	{
  	  byte d = 6;
  	  // 0 x
      // 1 y
      // 2 z
      // 3 -x
      // 4 -y
      // 5 -z

		  if (p.x > max.x)
		    d = 0;
		  else if (p.x < 0)
		    d = 3;
      else if (p.y > max.y)
  	    d = 1;
	    else if (p.y < 0)
		    d = 4;
	    else if (p.z > max.z)
		    d = 2;
	    else if (p.z < 0)
		    d = 5;
	    else
	    {
	      Debug.LogError("Oops, something went wrong with expansion.");
	      return index;
      }
      ExpandBounds(d, expansionAmount);
  	}

	  index = VoxelVolumeAssembler.GetIndex(vol, point, false);    // Offset is applied in vol.GetIndex(). false = don't convert to loca coords (already converted)

	  if (index == -1)
	  {
	    Debug.LogError("Something gone wrong");
	    return index;
	  }

  	AddVoxel( index, colorIndex );
    return index;
  }

  // --- Only the index is known ---
  void AddVoxel( int index, Color color )
  {
    byte colorIndex = AddColor( color );   // Add the new color
    AddVoxel( index, colorIndex );   // Calls the function below
  }

  // --- Both are known ---
  void AddVoxel( int index, byte colorIndex )
  {
     //Debug.Log("colorIndex: "+colorIndex+". colorPalette.Count: "+vol.colorPalette.Count+". Index: "+index+". data.Length: "+vol.data.Length);
     
    try{
      vol.data[index] = colorIndex;
      changesMade = true;
    }
    catch (System.SystemException e)
    {
      Debug.LogError("Bad access at index "+index+" of "+vol.data.Length+"  "+e);
    }
     
  }




  void ExpandBounds( byte direction, int amount )
  {
    //Debug.Log("Expanding in "+direction+" direction...");

    ushort[] newDimensions = new ushort[3];
    byte[] newDataField = new byte[1];

    switch (direction)
    {
      case 0:   // 'amount' along +x
        newDimensions[0] = (ushort)(vol.dimensions[0]+amount);
        newDimensions[1] = vol.dimensions[1];
        newDimensions[2] = vol.dimensions[2];
        newDataField = new byte[newDimensions[0]*newDimensions[1]*newDimensions[2]];
        //vol.centerOffset[0] += amount;

        for (ulong i=0; i<(ulong)newDataField.Length; i++)
          newDataField[i] = 255;    // Set all to null value

        ApplyPositiveExpansion(newDataField, newDimensions);
      break;

      case 1:   // +y axis
        newDimensions[0] = vol.dimensions[0];
        newDimensions[1] = (ushort)(vol.dimensions[1]+amount);
        newDimensions[2] = vol.dimensions[2];
        newDataField = new byte[newDimensions[0]*newDimensions[1]*newDimensions[2]];
        //vol.centerOffset[1] += amount;

        for (ulong i=0; i<(ulong)newDataField.Length; i++)
          newDataField[i] = 255;

        ApplyPositiveExpansion(newDataField, newDimensions);
      break;

      case 2:   // +z axis
        newDimensions[0] = vol.dimensions[0];
        newDimensions[1] = vol.dimensions[1];
        newDimensions[2] = (ushort)(vol.dimensions[2]+amount);
        newDataField = new byte[newDimensions[0]*newDimensions[1]*newDimensions[2]];
        //vol.centerOffset[2] += amount;

        for (ulong i=0; i<(ulong)newDataField.Length; i++)
          newDataField[i] = 255;

        ApplyPositiveExpansion(newDataField, newDimensions);
      break;

      case 3:   // -x axis
        newDimensions[0] = (ushort)(vol.dimensions[0]+amount);
        newDimensions[1] = vol.dimensions[1];
        newDimensions[2] = vol.dimensions[2];
        newDataField = new byte[newDimensions[0]*newDimensions[1]*newDimensions[2]];
        vol.centerOffset[0] += amount;

        for (ulong i=0; i<(ulong)newDataField.Length; i++)
          newDataField[i] = 255;    // Set all to null value

        ApplyNegativeExpansion(newDataField, newDimensions, amount, 0, 0);
      break;

      case 4:   // -y axis
        newDimensions[0] = vol.dimensions[0];
        newDimensions[1] = (ushort)(vol.dimensions[1]+amount);
        newDimensions[2] = vol.dimensions[2];
        newDataField = new byte[newDimensions[0]*newDimensions[1]*newDimensions[2]];
        vol.centerOffset[1] += amount;

        for (ulong i=0; i<(ulong)newDataField.Length; i++)
          newDataField[i] = 255;    // Set all to null value

        ApplyNegativeExpansion(newDataField, newDimensions, 0, amount, 0);
      break;

      case 5:   // -z axis
        newDimensions[0] = vol.dimensions[0];
        newDimensions[1] = vol.dimensions[1];
        newDimensions[2] = (ushort)(vol.dimensions[2]+amount);
        newDataField = new byte[newDimensions[0]*newDimensions[1]*newDimensions[2]];
        vol.centerOffset[2] += amount;

        for (ulong i=0; i<(ulong)newDataField.Length; i++)
          newDataField[i] = 255;    // Set all to null value

        ApplyNegativeExpansion(newDataField, newDimensions, 0, 0, amount);
      break;
    }

    vol.dimensions = newDimensions;
    vol.data = newDataField;

    //DrawBounds();
  }

  void ApplyPositiveExpansion(byte[] newDataField, ushort[] newDimensions)
  {
    for (int x=0; x<vol.dimensions[0]; x++)
    {
      for (int y=0; y<vol.dimensions[1]; y++)
      {
        for (int z=0; z<vol.dimensions[2]; z++)
        {
          newDataField[VoxelVolumeAssembler.GetIndex(x,y,z, newDimensions)] = vol.data[VoxelVolumeAssembler.GetIndex(x,y,z, vol.dimensions)];
        }
      }
    }
  }

  void ApplyNegativeExpansion(byte[] newDataField, ushort[] newDimensions, int amountX, int amountY, int amountZ)
  {
    for (int x=0; x<vol.dimensions[0]; x++)
    {
      for (int y=0; y<vol.dimensions[1]; y++)
      {
        for (int z=0; z<vol.dimensions[2]; z++)
        {
          int newIndex = VoxelVolumeAssembler.GetIndex(amountX+x, amountY+y, amountZ+z, newDimensions);
          int index = VoxelVolumeAssembler.GetIndex(x,y,z, vol.dimensions);
          //Debug.Log("x: "+x+" y_"+y+" z_"+z+" index: "+index+" data.Length: "+vol.data.Length+" newIndex: "+newIndex+" newDataField.Length: "+newDataField.Length+" new dimensions cubed: "+newDimensions[0]*newDimensions[1]*newDimensions[2]);
          newDataField[newIndex] = vol.data[index];
        }
      }
    }
  }

  void FlipDataOverAxis( byte axis )
  {
    ushort[] newDimensions = new ushort[3];
    byte[] newDataField = new byte[1];

    switch (axis)
    {
      case 0:   // Right
        ushort xWidth = (ushort)(vol.dimensions[0]-1), yHeight = (ushort)(vol.dimensions[1]-1), zWidth = (ushort)(vol.dimensions[2]-1);

        newDataField = new byte[xWidth*yHeight*zWidth];

        for (ushort x=0; x<xWidth-1; x++)
        {
          for (ushort y=0; x<yHeight-1; y++)
          {
            for (ushort z=0; z<zWidth-1; z++)
            {
              int newIndex = VoxelVolumeAssembler.GetIndex(xWidth-x, y, x, vol.dimensions);
              int index = VoxelVolumeAssembler.GetIndex(x,y,z, vol.dimensions);

              #if VCE_DEBUG
                Debug.Log("x: "+x+" y_"+y+" z_"+z+" index: "+index+" data.Length: "+vol.data.Length+" newIndex: "+newIndex+" newDataField.Length: "+newDataField.Length+" new dimensions cubed: "+vol.dimensions[0]*vol.dimensions[1]*vol.dimensions[2]);
              #endif

              newDataField[newIndex] = vol.data[index];
            }
          }
        }
      break;

      default:
        return;
    }

    vol.dimensions = newDimensions;
    vol.data = newDataField;

    //DrawBounds();
  }

  byte AddColor (Color c)
  {
    int colorIndex = -1;
    byte i = 0;
    Color[] colors = (Color[])vol.colorPalette.ToArray();
    float cutoff = .1f;

    for (i=0; i<colors.Length; i++)
    {
/*      if (vol.colorPalette[i].a == 0)   // A "null" color
      {
        colorIndex = i;
        break;
      }*/

      // Color within a few points exists
      if (Mathf.Abs(colors[i].r - c.r) < cutoff
          && Mathf.Abs(colors[i].g - c.g) < cutoff
          && Mathf.Abs(colors[i].b - c.b) < cutoff)
      {
        //Debug.Log("colorPalette: "+vol.colorPalette[i]+".  c: "+c);
        colorIndex = i;
        break;
      }
    }

    if (colorIndex == -1)    // No space yet
    {
      //Debug.Log("Color not found. colorPalette.Count is "+vol.colorPalette.Count+". Color is "+c);

      //Debug.Log(vol.colorPalette.Count);
      if (vol.colorPalette.Count < 255)   // Is there space in whole?
      {
        vol.colorPalette.Add(c);
        colorIndex = vol.colorPalette.Count-1;   // Returns the first blank in the new bank
      }
      else
        Debug.LogError(" Too many colors in volume. ");
    }

    if (colorIndex != -1)
	  {
		  changesMade = true;
		  return (byte)colorIndex;
  	}
    else
      return 0;
  }

  public int ExpandPalette()
  {
    vol.colorPalette.Add(Color.white);

    return vol.colorPalette.Count;
  }

  public void BuildMeshAndTexture()
  {
    vol.texture = VoxelVolumeCreator.RebuildTexture( vol.colorPalette.ToArray() );

    if (vol.texture != null)
      vol.BuildMesh();
  }

  void OnDisable()
  {
    if (vol == null || vol.isPrefab)
      return;

    if (vol.autosaveOnFocusChange && vol.lastSavedFilePath != "")
      SaveBackup();

    EditorPrefs.SetString("lastObj", vol.gameObject.name);
  }

  void OnEnable()
  {
    if (!(vol==null) && vol.isPrefab)
      return;

    // Check the three fail conditions
    if ( vol==null      ||
          !initialized  ||
          EditorPrefs.GetString("lastObj")!=vol.gameObject.name )
    {
      Initialize();
    }

    if (vol.isPrefab)
      return;

    LoadBackup();

    VoxelColorPaletteWindow.OnFocus();
  }

  // ==============================================
  //            Volume From Image
  // ==============================================

  void VolumeFromImage()
  {
    string oldPath = vol.lastSavedFilePath;

    if (textureInputXY!=null && textureInputZY!=null && textureInputXY.height != textureInputZY.height)
    {
      Debug.LogError("textureInputXY and textureInputZY are not the same height. Can't make a volume from Image.");
      return;
    }

    int mid;

    if (textureInputXY==null)
    {
      vol.Reset( 1, (ushort)textureInputZY.height, (ushort)textureInputZY.width );
      mid = 0;
    }
    else if (textureInputZY==null)
    {
      vol.Reset( (ushort)textureInputXY.width, (ushort)textureInputXY.height, 1 );
      mid = 0;
    }
    else
    {
  	  vol.Reset( (ushort)textureInputXY.width, (ushort)textureInputXY.height, (ushort)textureInputZY.width );
  	  mid = (int)(vol.dimensions[2]/2f);
    }

	  vol.lastSavedFilePath = oldPath;
	  Initialize();

    for (int i=0; i<vol.data.Length; i++)
      vol.data[i] = 255;    // The null case

    Color c;

    // XY plane
    if (textureInputXY != null)
    {
      for (int x=0; x<textureInputXY.width; x++)
      {
        for (int y=0; y<textureInputXY.height; y++)
        {
          //Debug.Log(textureInput.GetPixel(x,y));
          c = textureInputXY.GetPixel(x,y);

          if (c.a > .5f)
            AddVoxel(VoxelVolumeAssembler.GetIndex(x,y,mid, vol.dimensions), c);
        }
      }
    }

    // ZY plane
    if (textureInputZY != null)
    {
      for (int z=0; z<textureInputZY.width; z++)
      {
        for (int y=0; y<textureInputZY.height; y++)
        {
          //Debug.Log(textureInput.GetPixel(x,y));
          c = textureInputZY.GetPixel(z,y);

          if (c.a == 1)
            AddVoxel(VoxelVolumeAssembler.GetIndex(mid,y,z, vol.dimensions), c);
        }
      }
    }
  }

  // ==============================================
  //  VoxelVolumeCreatorEditor save and load operations
  // ==============================================
  // --- Save ---
  void Save()
  {
    if (vol.lastSavedFilePath == "" || vol.lastSavedGameObjectName != vol.gameObject.name)
      SaveAs();
    else
      SaveToDisk(vol.lastSavedFilePath);
  }

  void SaveAs()
  {
    string s = EditorUtility.SaveFilePanel("Save new voxel volume data as", Application.dataPath+
      VoxelVolumeCreator.voxelDataDirectory, vol.gameObject.name, VoxelVolumeCreator.vDataExtension);
	  if (s != "")
	  {
			vol.lastSavedFilePath = s;
	    SaveToDisk(s);
		}
	}

  void SaveBackup()
  {
    #if VCE_DEBUG
    Debug.Log("VCE: Saving backup of "+vol.gameObject.name);
    #endif

    string s = vol.GetBackupPath();
    if (vol.lastBackupPath != "" && vol.lastBackupPath != s)
    {
      #if VCE_DEBUG
      Debug.Log("VCE: GameObject name changed. Destroying backup at "+vol.lastBackupPath);
      #endif
      DestroyVoxelData(vol.lastBackupPath);    // Gameobject name has changed
    }
    Debug.Log(s);

    if (SaveToDisk(s))
      vol.lastBackupPath = s;
  }

  bool SaveToDisk(string s)
  {
    PreSave();
    bool success = vol.DoSaveToDisk(s);
    if (success)
    {
      changesMade = false;
    }
    return success;
  }

  void PreSave()
  {
    TrimDimensions();
    BuildMeshAndTexture();

    if(vol.lastBackupPath != "")
      DestroyVoxelData(vol.lastBackupPath);
  }


  // --- Load ---
  void LoadAs()
  {
    string s = EditorUtility.OpenFilePanel("Load voxel volume data", Application.dataPath+
      VoxelVolumeCreator.voxelDataDirectory, VoxelVolumeCreator.vDataExtension);

    if (vol.lastBackupPath != "")
      if (DestroyVoxelData(vol.lastBackupPath))
        vol.lastBackupPath = "";

    LoadFromFile(s);
  }
  
  void LoadBackup()
  {
    #if VCE_DEBUG
    Debug.Log("VCE: Detecting focus switch. Loading backup....");
    #endif

    string path = vol.lastBackupPath;
    if (path == "")
    {
      if (vol.lastSavedFilePath != "")
        path = vol.lastSavedFilePath;
      else
        path = vol.GetBackupPath();
    }
    
    if (!System.IO.File.Exists(path))
      return;

    LoadFromFile(path);
  }

  void LoadFromFile( string s )
  {
    vol.DoLoadFromFile(s);
    vol.lastSavedFilePath = s;
    BuildMeshAndTexture();
  }

  bool DestroyVoxelData(string path)
  {
    if(System.IO.File.Exists(path))
    {
      System.IO.File.Delete(path);

      // Remove meta file, if applicable
      if(System.IO.File.Exists(path+".meta"))
        System.IO.File.Delete(path+".meta");

      return true;
    }
    else
    {
      #if VCE_DEBUG
        Debug.LogWarning("Can't destroy voxel data. File not found: "+path);
      #endif
      return false;
    }

  }

}
