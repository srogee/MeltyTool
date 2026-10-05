using System.Xml.Linq;

using fin.io;
using fin.io.bundles;
using fin.util.progress;

using hpcos.api;
using hpcos.src.api;

namespace uni.games.hp_chamber_of_secrets;

// https://wiki.dolphin-emu.org/index.php?title=GHSE69
[GameIDs("GHSY69", "GHSJ13", "GHSE69", "GHSP69", "GHSX69")]
public sealed class HPChamberOfSecretsFileBundleGatherer
    : BGameCubeFileBundleGatherer {
  public override string Name => "hp_chamber_of_secrets";
  public override string Title => "Harry Potter and the Chamber of Secrets";

  protected override void GatherFileBundlesFromHierarchy(
      IFileBundleOrganizer organizer,
      IMutablePercentageProgress mutablePercentageProgress,
      IFileHierarchy fileHierarchy) {
    // Organize files into folders for easier viewing
    OrganizeFiles_(fileHierarchy);

    // Gather supported files
    GatherFiles(organizer, fileHierarchy);
  }

  private static void OrganizeFiles_(IFileHierarchy fileHierarchy) {
    ISystemDirectory baseDir = fileHierarchy.Root.Impl;
    bool anyFilesMoved = false;

    foreach (IFileHierarchyDirectory directory in fileHierarchy) {
      IFileHierarchyFile[] geoFiles = [.. directory.FilesWithExtension(".geo")]; // Snapshot files just in case moving them while iterating causes issues
      foreach (IFileHierarchyFile geoFile in geoFiles) {
        string name = geoFile.NameWithoutExtension.ToString();
        string categoryPath = GeoModelFileOrganizer.GetCategory(name);

        IReadOnlyTreeDirectory parent = geoFile.AssertGetParent();
        string categoryFullPath = Path.Join(baseDir.FullPath, categoryPath);
        if (PathsEqual_(parent.FullPath, categoryFullPath)) {
          // File is already at the expected category path
          continue;
        }

        // File is not at the expected category path
        ISystemDirectory categoryDir = baseDir.GetOrCreateSubdir(categoryPath); // Ensure each folder in the category path has been created

        if (directory.TryToGetExistingFile(name + ".gdt", out IFileHierarchyFile? gdtFile)) {
          // Move associated .gdt file first
          FinFileSystem.File.Move(gdtFile.FullPath, Path.Join(categoryDir.FullPath, gdtFile.Name), true);
        }

        // Now move the .geo file
        FinFileSystem.File.Move(geoFile.FullPath, Path.Join(categoryDir.FullPath, geoFile.Name), true);

        anyFilesMoved = true;
      }
    }

    if (anyFilesMoved) {
      fileHierarchy.RefreshRootAndUpdateCache();
    }
  }

  // Finds all .geo files, associates them with a .gdt file if one exists, and adds them to the organizer
  private static void GatherFiles(IFileBundleOrganizer organizer, IFileHierarchy fileHierarchy) {
    foreach (IFileHierarchyDirectory directory in fileHierarchy) {
      IEnumerable<GeoModelFileBundle> bundles = directory.FilesWithExtension(".geo").Where(g => GeoModelFileOrganizer.GetCategory(g.NameWithoutExtension.ToString()) != null).Select(f => GeoModelFileBundle.Create(directory, f));
      foreach (GeoModelFileBundle bundle in bundles) {
        organizer.Add(bundle);
      }
    }
  }

  private static bool PathsEqual_(string a, string b) {
    return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
                                                                         Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
                                                                         StringComparison.OrdinalIgnoreCase);
  }
}
