using fin.image;

using gx;
using gx.image;

using schema.binary;

namespace hpcos.schema.geo;

public enum GeoTextureFormat {
  RGB5A3 = 1,
  CMPR = 2,
  RGBA8 = 3,
}

public sealed class GeoTexture : IBinaryDeserializable {
  public const int SIZE = 0xbc;

  public long Offset { get; private set; }
  public int Width { get; private set; }
  public int Height { get; private set; }
  public string Name { get; private set; }
  public GeoTextureFormat Format { get; private set; }

  /// <summary>
  ///   Data for the top mip level, padded out to whole tiles.
  /// </summary>
  public byte[] ImageData { get; private set; }

  public void Read(IBinaryReader br) {
    this.Offset = br.Position;

    this.Width = br.ReadInt32At(this.Offset + 0x30);
    this.Height = br.ReadInt32At(this.Offset + 0x34);
    this.Name = br.ReadRelativeStringAt(this.Offset + 0x3c);
    this.Format = (GeoTextureFormat) br.ReadInt32At(this.Offset + 0x4c);
    long dataOffset = br.ReadRelativePointerAt(this.Offset + 0x58);

    // The length field is sometimes 0, so the size of the top mip level is
    // calculated from the dimensions instead.
    int dataLength = this.Format switch {
      GeoTextureFormat.CMPR => (this.Width + 7) / 8 *
                                 ((this.Height + 7) / 8) *
                                 32,
      GeoTextureFormat.RGBA8 => (this.Width + 3) / 4 *
                                ((this.Height + 3) / 4) *
                                64,
      _ => (this.Width + 3) / 4 * ((this.Height + 3) / 4) * 32,
    };

    // Some textures run off the end of the file, so pad them with zeroes.
    this.ImageData = new byte[dataLength];
    br.Position = dataOffset;
    int availableLength
        = (int) Math.Clamp(br.Length - dataOffset, 0, dataLength);
    br.ReadBytes(this.ImageData.AsSpan(0, availableLength));

    br.Position = this.Offset + SIZE;
  }

  public IImage ToImage() {
    GxTextureFormat gxFormat = this.Format switch {
      GeoTextureFormat.RGB5A3 => GxTextureFormat.A3_RGB5,
      GeoTextureFormat.CMPR => GxTextureFormat.S3TC1,
      GeoTextureFormat.RGBA8 => GxTextureFormat.ARGB8,
      _ => throw new ArgumentOutOfRangeException(
          nameof(this.Format),
          this.Format,
          null)
    };

    return new GxImageReader(this.Width, this.Height, gxFormat).ReadImage(
        this.ImageData,
        Endianness.BigEndian);
  }
}
