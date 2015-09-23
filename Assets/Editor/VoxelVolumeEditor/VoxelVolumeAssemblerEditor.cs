using UnityEditor;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(VoxelVolumeAssembler))]
public class VoxelVolumeAssemblerEditor : Editor
{
  VoxelVolumeAssembler builder;
  bool initialized;
  
  void Initialize()
  {
    builder = (VoxelVolumeAssembler)target;
    
    initialized = true;
  }
  
	public override void OnInspectorGUI ()
	{
		if (!initialized)
		  Initialize();
    
		GUILayout.Label("VoxelVolumeAssembler is designed to render objects made with VolumeCreator");
    
    builder.workingFile = EditorGUILayout.ObjectField(builder.workingFile, typeof(UnityEngine.Object), false );   // Don't allow scene objects
    
    GUILayout.Label("Current path: "+builder.pathToFile);
    
		// if (GUILayout.Button("(Re)Build"))
		// 	builder.BuildFromBinary(AssetDatabase.GetAssetPath(builder.workingFile));
	  
	  // @TODO: RGB file interface
	}
}