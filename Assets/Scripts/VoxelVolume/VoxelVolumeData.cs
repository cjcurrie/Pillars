using System;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

using System.Xml;
using System.Xml.Serialization;

[Serializable ()]
public class VoxelVolumeData : ISerializable {
  
  [XmlArray("data")]
  public byte[] data;

  [XmlArray("colors")]
  public float[,] colors;

  [XmlArray("dimensions")]
  public ushort[] dimensions;

  [XmlAttribute("voxelScale")]
  public float voxelScale;

  [XmlArray("centerOffset")]
  public int[] centerOffset;
  
  public VoxelVolumeData ( byte[] data_in, float[,] colors_in, ushort[] dimensions_in, float voxelScale_in, int[] centerOffset_in ) 
  {
    data = data_in;
    colors = colors_in;
    dimensions = dimensions_in;
    voxelScale = voxelScale_in;
    centerOffset = centerOffset_in;
  }
  
  public VoxelVolumeData (SerializationInfo info, StreamingContext ctxt)
  {
    data = (byte[])info.GetValue("data", typeof(byte[]));
    colors = (float[,])info.GetValue("colors", typeof(float[,]));
    dimensions = (ushort[])info.GetValue("dimensions", typeof(ushort[]));
    voxelScale = (float)info.GetValue("voxelScale", typeof(float));
    centerOffset = (int[])info.GetValue("centerOffset", typeof(int[]));
  }

  public void GetObjectData (SerializationInfo info, StreamingContext ctxt)
  {
    info.AddValue("data", data);
    info.AddValue("colors", colors);
    info.AddValue("dimensions", dimensions);
    info.AddValue("voxelScale", voxelScale);
    info.AddValue("centerOffset", centerOffset);
  }
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