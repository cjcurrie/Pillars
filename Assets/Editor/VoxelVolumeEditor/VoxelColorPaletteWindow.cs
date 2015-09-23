using UnityEngine;
using UnityEditor;

public class VoxelColorPaletteWindow : EditorWindow
{
  public static bool initialized;
  
  static VoxelVolumeCreator vol;
  static VoxelColorPaletteWindow myWindow;
  static VoxelVolumeCreatorEditor callback;
  
  static Vector2 scrollPosition = Vector2.zero;

  static int colorEntriesAcross;

  public static void OnFocus()
  {
    if (!initialized)
      return;
      
    myWindow.Focus();
  }

  public static void Initialize (VoxelVolumeCreator volume_in, VoxelVolumeCreatorEditor callback_in)
  {
    vol = volume_in;
    callback = callback_in;
    //scrollPosition = Vector2.zero;
    
    // Get or create
    if (myWindow == null)
    {
      myWindow = (VoxelColorPaletteWindow)EditorWindow.GetWindow (typeof (VoxelColorPaletteWindow));   // Returns the window
    }
    //myWindow.ShowUtility();
    //myWindow.ShowAuxWindow();
    initialized = true;

    colorEntriesAcross = (int)(myWindow.position.width/200f);
  }
  
  public static void DoClose()
  {
    myWindow.Close();
  }

    
  void OnGUI()
  {
    if (vol == null || vol.colorPalette == null)
      return;
      
    GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Add another palette color"))
        callback.ExpandPalette();
      if (GUILayout.Button("Apply colors/rebuild mesh"))
        callback.BuildMeshAndTexture();
      GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();
    
    scrollPosition = GUILayout.BeginScrollView( scrollPosition );
    
      for (byte y=0; y<(vol.colorPalette.Count/4)+1; y++)
      {
        GUILayout.BeginHorizontal();
          for (byte x=0; x<colorEntriesAcross; x++)
          {
            byte index = (byte)(x + colorEntriesAcross*y);
            
            if (index > 255)    // We don't want position number 256 (null) being used
              continue;
              
            if (index > vol.colorPalette.Count - 1)
              break;
            
            GUILayout.BeginVertical(GUILayout.MaxWidth(120));
                
              Color t = vol.colorPalette[index];
              vol.colorPalette[index] = EditorGUILayout.ColorField( "", vol.colorPalette[index] );
              if (t != vol.colorPalette[index])   // Color was changed
                vol.selectedColor = index;
              
              GUILayout.BeginHorizontal();
              //GUILayout.FlexibleSpace();
              GUILayout.Label("Color "+index);
                if (index == vol.selectedColor)
                {
                  GUILayout.Box("Currently Selected");
                }
                else
                {
                  if (GUILayout.Button("Select Color"))
                    vol.selectedColor = index;
                }
              GUILayout.EndHorizontal();
              
            GUILayout.EndVertical();
            
            
          }
        GUILayout.EndHorizontal();
      }
      
    GUILayout.EndScrollView();
  }
}