using schema.binary;

namespace hpcos.schema;

/// <summary>
///   GEO/GDT files use self-relative pointers: an int32 whose value is an
///   offset from the position of the pointer itself.
/// </summary>
public static class RelativePointerExtensions {
  public static long ReadRelativePointer(this IBinaryReader br) {
    return br.Position + br.ReadInt32();
  }

  public static long ReadRelativePointerAt(this IBinaryReader br,
                                           long position) {
    br.Position = position;
    return br.ReadRelativePointer();
  }

  public static string ReadRelativeStringAt(this IBinaryReader br,
                                            long position) {
    long stringOffset = br.ReadRelativePointerAt(position);
    return br.SubreadAt(stringOffset, () => br.ReadStringNT());
  }

  public static int ReadInt32At(this IBinaryReader br, long position) {
    br.Position = position;
    return br.ReadInt32();
  }
}
