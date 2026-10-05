using fin.io;

using schema.binary;

namespace hpcos.schema.geo;

/// <summary>
///   Header of a GameCube GEO file. Meshes aren't read here, since the GEO's
///   mesh table only contains the meshes that aren't stored in its GDT.
/// </summary>
public sealed class Geo : IBinaryDeserializable {
  public const byte SUPPORTED_VERSION = 0x10;

  public byte Version { get; private set; }

  /// <summary>
  ///   Total number of meshes, including those stored in the GDT.
  /// </summary>
  public int TotalMeshCount { get; private set; }

  public long MeshesOffset { get; private set; }

  public GeoTexture[] Textures { get; private set; }
  public GeoLayoutObject[] LayoutObjects { get; private set; }
  public GeoSubfile[] Subfiles { get; private set; }

  // Gets the version of a Geo file
  public static byte GetVersion(IFileHierarchyFile geoFile) {
    using IBinaryReader br = geoFile.OpenReadAsBinary();
    br.Position = 0x06;
    return br.ReadByte();
  }

  public void Read(IBinaryReader br) {
    br.Position = 0x06;
    this.Version = br.ReadByte();
    if (this.Version != SUPPORTED_VERSION) {
      throw new NotSupportedException(
          $"Unsupported GEO version: 0x{this.Version:X2}");
    }

    this.TotalMeshCount = br.ReadInt32At(0x0c);
    this.MeshesOffset = br.ReadRelativePointerAt(0x10);

    int textureCount = br.ReadInt32At(0x14);
    long texturesOffset = br.ReadRelativePointerAt(0x18);
    this.Textures = new GeoTexture[textureCount];
    for (int i = 0; i < textureCount; ++i) {
      br.Position = texturesOffset + i * GeoTexture.SIZE;
      this.Textures[i] = br.ReadNew<GeoTexture>();
    }

    // Only the first layout is used.
    this.LayoutObjects = [];
    int layoutCount = br.ReadInt32At(0x2c);
    long layoutsOffset = br.ReadRelativePointerAt(0x30);
    if (layoutCount > 0) {
      int objectCount = br.ReadInt32At(layoutsOffset + 0x64);
      long objectsOffset = br.ReadRelativePointerAt(layoutsOffset + 0x68);
      this.LayoutObjects = new GeoLayoutObject[objectCount];
      for (int i = 0; i < objectCount; ++i) {
        br.Position = objectsOffset + i * GeoLayoutObject.SIZE;
        this.LayoutObjects[i] = br.ReadNew<GeoLayoutObject>();
      }
    }

    int subfileCount = br.ReadInt32At(0x64);
    long subfilesOffset = br.ReadRelativePointerAt(0x68);
    this.Subfiles = new GeoSubfile[subfileCount];
    for (int i = 0; i < subfileCount; ++i) {
      br.Position = subfilesOffset + i * GeoSubfile.SIZE;
      this.Subfiles[i] = br.ReadNew<GeoSubfile>();
    }
  }
}
