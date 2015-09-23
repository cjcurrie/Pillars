using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.IO;
using UnityEngine;

public static class VoxelVolumeBinaryHandler
{
  public static void WriteToDisk(string filePath, VoxelVolumeData data) {
    writeData(data, filePath);
  }

  static void writeData(VoxelVolumeData data, string path) {
    Stream stream = File.Open(path, FileMode.Create);
    BinaryFormatter bformatter = new BinaryFormatter();
    bformatter.Binder = new VersionDeserializationBinder();
    bformatter.Serialize(stream, data);
    stream.Close();
  }

  public static VoxelVolumeData ReadFromDisk(string filePath)
  {
    return readData(filePath);
  }

  static VoxelVolumeData readData(string path)
  {
    VoxelVolumeData output;
    Stream stream = File.Open(path, FileMode.Open);
    BinaryFormatter bformatter = new BinaryFormatter();
    bformatter.Binder = new VersionDeserializationBinder(); 
    output = (VoxelVolumeData)bformatter.Deserialize(stream);
    stream.Close();

    return output;
  }


  public sealed class VersionDeserializationBinder : SerializationBinder
  {
      public override Type BindToType( string assemblyName, string typeName )
      {
          if ( !string.IsNullOrEmpty( assemblyName ) && !string.IsNullOrEmpty( typeName ) )
          {
              Type typeToDeserialize = null;

              assemblyName = Assembly.GetExecutingAssembly().FullName;

              // The following line of code returns the type.
              typeToDeserialize = Type.GetType( String.Format( "{0}, {1}", typeName, assemblyName ) );

              return typeToDeserialize;
          }

          return null;
      }
  }
}
