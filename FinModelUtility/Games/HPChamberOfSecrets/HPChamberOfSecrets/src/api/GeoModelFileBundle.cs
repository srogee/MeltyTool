using fin.io;
using fin.model.io;

namespace hpcos.api;

// Class that bundles .geo files with their associated .gdt
public sealed record GeoModelFileBundle(
    IReadOnlyTreeFile GeoFile,
    IReadOnlyTreeFile? GdtFile)
    : IModelFileBundle<GeoModelFileBundle, GeoModelImporter> {
  public IReadOnlyTreeFile MainFile => this.GeoFile;

  public static GeoModelFileBundle Create(IFileHierarchyDirectory directory, IFileHierarchyFile geoFile) {
    directory.TryToGetExistingFile(geoFile.NameWithoutExtension.ToString() + ".gdt", out IFileHierarchyFile? gdtFile);
    return new GeoModelFileBundle(geoFile, gdtFile);
  }
}
