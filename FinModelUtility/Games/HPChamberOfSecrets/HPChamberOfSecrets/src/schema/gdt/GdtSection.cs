using hpcos.schema.geo;

using schema.binary;

namespace hpcos.schema.gdt;

/// <summary>
///   A section of a GDT file, as referenced by one of its GEO's subfiles.
/// </summary>
public sealed class GdtSection : IBinaryDeserializable {
  public GeoMesh[] Meshes { get; private set; }

  public void Read(IBinaryReader br) {
    long offset = br.Position;

    int meshCount = br.ReadInt32At(offset + 0x18);
    long meshesOffset = br.ReadRelativePointerAt(offset + 0x1c);

    this.Meshes = new GeoMesh[meshCount];
    for (int i = 0; i < meshCount; ++i) {
      br.Position = meshesOffset + i * GeoMesh.SIZE;
      this.Meshes[i] = br.ReadNew<GeoMesh>();
    }
  }
}
