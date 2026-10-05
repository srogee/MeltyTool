using System.Globalization;
using System.Numerics;

using schema.binary;

namespace hpcos.schema.geo;

[Flags]
public enum GeoVertexFormat {
  /// <summary>
  ///   Positions are floats, otherwise s16s with PositionShift fractional bits.
  /// </summary>
  FLOAT_POSITIONS = 0x01,

  /// <summary>
  ///   Each position is followed by an s16 normal with 14 fractional bits.
  /// </summary>
  S16_NORMALS = 0x04,

  /// <summary>
  ///   Each position is followed by an s8 normal with 6 fractional bits + 1
  ///   byte of padding.
  /// </summary>
  S8_NORMALS = 0x08,

  FLOAT_TEXCOORDS = 0x10,

  /// <summary>
  ///   Texcoords are s16s with 13 fractional bits.
  /// </summary>
  S16_TEXCOORDS = 0x20,
}

public sealed class GeoMesh : IBinaryDeserializable {
  public const int SIZE = 0xec;

  public string Name { get; private set; }

  /// <summary>
  ///   Index of this mesh within the GEO's mesh table, which is only stored
  ///   as the "_X" suffix of the mesh's name.
  /// </summary>
  public int Index { get; private set; }

  public GeoVertexFormat VertexFormat { get; private set; }

  public Vector3[] Positions { get; private set; }

  /// <summary>
  ///   Per-position normals. Null if the mesh has no normals.
  /// </summary>
  public Vector3[]? Normals { get; private set; }

  public Vector2[] Texcoords { get; private set; }

  /// <summary>
  ///   Per-vertex RGBA colors, indexed by the texcoord index of the first
  ///   texture layer. Null if the mesh has no colors.
  /// </summary>
  public byte[]? Colors { get; private set; }

  public GeoStrip[] Strips { get; private set; }

  /// <summary>
  ///   Whether this mesh's strips are stored in a layout that isn't supported
  ///   yet (used by older GEOs).
  /// </summary>
  public bool HasUnsupportedStripLayout { get; private set; }

  public void Read(IBinaryReader br) {
    long offset = br.Position;

    this.Name = br.ReadRelativeStringAt(offset + 0x04);
    int positionCount = br.ReadInt32At(offset + 0x0c);
    long positionsOffset = br.ReadRelativePointerAt(offset + 0x10);
    long colorsOffset = br.ReadRelativePointerAt(offset + 0x14);
    int colormapCount = br.ReadInt32At(offset + 0x74);
    long texcoordsOffset = br.ReadRelativePointerAt(offset + 0x98);
    this.VertexFormat = (GeoVertexFormat) br.ReadInt32At(offset + 0xc4);
    int positionShift = br.ReadInt32At(offset + 0xc8);
    int stripGroupCount = br.ReadInt32At(offset + 0xcc);
    long stripGroupsOffset = br.ReadRelativePointerAt(offset + 0xd0);
    int texcoordCount = br.ReadInt32At(offset + 0xe0);

    this.Index = int.Parse(this.Name[(this.Name.LastIndexOf('_') + 1)..],
                           CultureInfo.InvariantCulture);

    this.ReadPositions_(br, positionsOffset, positionCount, positionShift);
    this.ReadTexcoords_(br, texcoordsOffset, texcoordCount);

    // Only the first colormap is used, the rest appear to be alternate
    // lighting setups.
    this.Colors = null;
    if (positionCount > 0 && colormapCount > 0) {
      br.Position = colorsOffset;
      this.Colors = br.ReadBytes(4 * texcoordCount);
    }

    this.ReadStrips_(br, positionCount, stripGroupsOffset, stripGroupCount);

    br.Position = offset + SIZE;
  }

  private void ReadPositions_(IBinaryReader br,
                              long positionsOffset,
                              int positionCount,
                              int positionShift) {
    bool hasFloatPositions
        = this.VertexFormat.HasFlag(GeoVertexFormat.FLOAT_POSITIONS);
    bool hasS16Normals
        = this.VertexFormat.HasFlag(GeoVertexFormat.S16_NORMALS);
    bool hasS8Normals
        = !hasS16Normals &&
          this.VertexFormat.HasFlag(GeoVertexFormat.S8_NORMALS);
    int normalSize = hasS16Normals ? 6 : hasS8Normals ? 4 : 0;
    int positionSize = (hasFloatPositions ? 12 : 6) + normalSize;

    float positionScale = 1f / (1 << positionShift);

    this.Positions = new Vector3[positionCount];
    this.Normals = normalSize > 0 ? new Vector3[positionCount] : null;
    for (int i = 0; i < positionCount; ++i) {
      br.Position = positionsOffset + i * positionSize;
      this.Positions[i] = hasFloatPositions
          ? new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
          : new Vector3(br.ReadInt16(), br.ReadInt16(), br.ReadInt16()) *
            positionScale;

      if (hasS16Normals) {
        this.Normals![i]
            = new Vector3(br.ReadInt16(), br.ReadInt16(), br.ReadInt16()) /
              16384f;
      } else if (hasS8Normals) {
        this.Normals![i]
            = new Vector3(br.ReadSByte(), br.ReadSByte(), br.ReadSByte()) /
              64f;
      }
    }
  }

  private void ReadTexcoords_(IBinaryReader br,
                              long texcoordsOffset,
                              int texcoordCount) {
    this.Texcoords = new Vector2[texcoordCount];
    if (this.Positions.Length == 0) {
      return;
    }

    br.Position = texcoordsOffset;
    if (this.VertexFormat.HasFlag(GeoVertexFormat.S16_TEXCOORDS)) {
      for (int i = 0; i < texcoordCount; ++i) {
        this.Texcoords[i] = new Vector2(br.ReadInt16(), br.ReadInt16()) /
                            8192f;
      }
    } else if (this.VertexFormat.HasFlag(GeoVertexFormat.FLOAT_TEXCOORDS)) {
      for (int i = 0; i < texcoordCount; ++i) {
        this.Texcoords[i] = new Vector2(br.ReadSingle(), br.ReadSingle());
      }
    }
  }

  private void ReadStrips_(IBinaryReader br,
                           int positionCount,
                           long stripGroupsOffset,
                           int stripGroupCount) {
    this.Strips = [];
    this.HasUnsupportedStripLayout = false;

    // Meshes without positions are collision meshes, which aren't supported
    // yet.
    if (positionCount == 0) {
      return;
    }

    List<GeoStrip> strips = [];
    for (int i = 0; i < stripGroupCount; ++i) {
      long stripGroupOffset = stripGroupsOffset + i * 0x1c;
      int stripCount = br.ReadInt32At(stripGroupOffset);
      long stripOffset = br.ReadRelativePointerAt(stripGroupOffset + 0x10);

      // TODO: Older GEOs (QRL2, Trollmap) store their strips differently.
      if (stripOffset == stripGroupOffset + 0x10) {
        this.HasUnsupportedStripLayout = true;
        return;
      }

      br.Position = stripOffset;
      for (int j = 0; j < stripCount; ++j) {
        strips.Add(br.ReadNew<GeoStrip>());
      }
    }

    this.Strips = strips.ToArray();
  }
}
