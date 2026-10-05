using System.Numerics;

using fin.data.lazy;
using fin.image;
using fin.image.util;
using fin.io;
using fin.language.equations.fixedFunction;
using fin.math.rotations;
using fin.math.transform;
using fin.model;
using fin.model.impl;
using fin.model.io.importers;
using fin.model.util;
using fin.util.sets;

using hpcos.schema.gdt;
using hpcos.schema.geo;

using schema.binary;

namespace hpcos.api;

/// <summary>
///   Importer for GameCube GEO models, based on lumos by pixel#8843.
///   https://github.com/metallicafan212/HarryPotterUnrealWiki/blob/master/Other/Console%20Model%20and%20Texture%20Exporter/lumos.py
///   Only supports static meshes for now.
/// </summary>
public sealed class GeoModelImporter
    : IModelImporter<GeoModelImporter, GeoModelFileBundle> {
  /// <summary>
  ///   The layer flags that affect how a material is rendered; any others are
  ///   ignored so they don't create redundant materials.
  /// </summary>
  private const GeoLayerFlags SUPPORTED_LAYER_FLAGS_
      = GeoLayerFlags.ENVIRONMENT_MAP |
        GeoLayerFlags.ALPHA_BLEND |
        GeoLayerFlags.DOUBLE_SIDED |
        GeoLayerFlags.ADDITIVE |
        GeoLayerFlags.SUBTRACTIVE |
        GeoLayerFlags.ALPHA_TEST |
        GeoLayerFlags.UV_SCROLL;

  /// <summary>
  ///   Where the scroll speed is stored isn't known yet, so this is just a
  ///   guess that looks reasonable for water/lava.
  /// </summary>
  private readonly Vector2 UV_SCROLL_SPEED = new(0.05f, 0.0f);

  private readonly record struct TextureKey(
      int TextureIndex,
      int UvIndex,
      bool EnvironmentMap,
      bool UvScroll);

  /// <summary>
  ///   MaskTextureIndex is -1 unless the strip is a terrain blend, see
  ///   AddMesh_().
  /// </summary>
  private readonly record struct MaterialKey(
      int TextureIndex,
      int MaskTextureIndex,
      GeoLayerFlags Flags,
      bool HasVertexColors);

  public IModel Import(GeoModelFileBundle fileBundle) {
    IReadOnlyTreeFile geoFile = fileBundle.GeoFile;
    IReadOnlyTreeFile? gdtFile = fileBundle.GdtFile;

    using IBinaryReader geoBr = geoFile.OpenReadAsBinary(Endianness.BigEndian);
    Geo geo = geoBr.ReadNew<Geo>();

    // Meshes in the GDT are counted as part of the GEO's total mesh count, so
    // they need to be read first to know how many meshes the GEO itself has.
    List<GeoMesh> geoMeshes = [];
    int geoMeshCount = geo.TotalMeshCount;
    if (geo.Subfiles.Length > 1) {
      if (gdtFile == null) {
        throw new FileNotFoundException(
            $"GEO has subfiles, but no accompanying GDT was found for {geoFile.DisplayFullPath}!");
      }

      using IBinaryReader gdtBr = gdtFile.OpenReadAsBinary(Endianness.BigEndian);
      foreach (GeoSubfile? subfile in geo.Subfiles.Skip(1)) {
        gdtBr.Position = subfile.GdtOffset;
        GdtSection gdtSection = gdtBr.ReadNew<GdtSection>();
        geoMeshes.AddRange(gdtSection.Meshes);
        geoMeshCount -= gdtSection.Meshes.Length;
      }
    }

    for (int i = 0; i < geoMeshCount; ++i) {
      geoBr.Position = geo.MeshesOffset + i * GeoMesh.SIZE;
      geoMeshes.Add(geoBr.ReadNew<GeoMesh>());
    }

    Dictionary<int, GeoMesh> geoMeshesByIndex = geoMeshes.ToDictionary(m => m.Index);

    HashSet<IReadOnlyStandaloneFile> files = geoFile.AsFileSet();
    if (gdtFile != null) {
      files.Add(gdtFile);
    }

    ModelImpl finModel = new() {
      FileBundle = fileBundle,
      Files = files,
    };

    IMaterialManager finMaterialManager = finModel.MaterialManager;
    // UV generation and scrolling are properties of the texture rather than
    // the material, so separate textures are created for each combination,
    // sharing the same image.
    LazyDictionary<int, IReadOnlyImage> lazyImages
        = new(textureIndex => geo.Textures[textureIndex].ToImage());
    LazyDictionary<TextureKey, ITexture> lazyFinTextures = new(key => {
      (int textureIndex, int uvIndex, bool environmentMap, bool uvScroll)
          = key;

      GeoTexture geoTexture = geo.Textures[textureIndex];
      string name = geoTexture.Name;
      if (name.Length == 0) {
        name = $"{geoFile.NameWithoutExtension}_{geoTexture.Offset}";
      }

      IReadOnlyImage image = lazyImages[textureIndex];
      ITexture finTexture = uvScroll
          ? finMaterialManager.CreateScrollingTexture(image,
                                                      UV_SCROLL_SPEED.X,
                                                      UV_SCROLL_SPEED.Y)
          : finMaterialManager.CreateTexture(image);
      finTexture.Name = name;
      finTexture.UvIndex = uvIndex;
      finTexture.WrapModeU = finTexture.WrapModeV = WrapMode.REPEAT;
      if (environmentMap) {
        finTexture.UvType = UvType.SPHERICAL;
      }

      return finTexture;
    });

    // The same texture can be drawn with different blending in different
    // strips, so a separate material is created for each combination.
    LazyDictionary<MaterialKey, IMaterial?> lazyFinMaterials = new(key => {
      (int textureIndex, int maskTextureIndex, GeoLayerFlags flags,
       bool hasVertexColors) = key;
      if (textureIndex < 0 || textureIndex >= geo.Textures.Length) {
        return null;
      }

      bool hasMask = maskTextureIndex >= 0 &&
                     maskTextureIndex < geo.Textures.Length;
      ITexture finTexture = lazyFinTextures[
          new TextureKey(textureIndex,
                         hasMask ? 1 : 0,
                         flags.HasFlag(GeoLayerFlags.ENVIRONMENT_MAP),
                         flags.HasFlag(GeoLayerFlags.UV_SCROLL))];
      IFixedFunctionMaterial finMaterial
          = finMaterialManager.AddFixedFunctionMaterial();
      finMaterial.Name = finTexture.Name;
      finMaterial.CullingMode = flags.HasFlag(GeoLayerFlags.DOUBLE_SIDED)
          ? CullingMode.SHOW_BOTH
          : CullingMode.SHOW_FRONT_ONLY;
      finMaterial.Shininess = MaterialConstants.DISABLED_SHININESS;

      IFixedFunctionEquations<FixedFunctionSource> equations
          = finMaterial.Equations;
      (IColorValue color, IScalarValue alpha) outputColorAlpha
          = finMaterial.GenerateDiffuse(
              (equations.ColorOps.One, equations.ScalarOps.One),
              finTexture,
              (hasVertexColors, false));

      // Lighting is baked into the vertex colors, where 0x80 is full
      // brightness, so they're doubled to let them overbrighten the
      // texture like in-game.
      if (hasVertexColors) {
        outputColorAlpha.color
            = equations.ColorOps.MultiplyWithConstant(
                outputColorAlpha.color,
                2);
      }

      // The mask's color is always black, so it only provides the alpha.
      if (hasMask) {
        ITexture finMaskTexture
            = lazyFinTextures[new TextureKey(maskTextureIndex, 0, false, false)];
        finMaterial.Name = $"{finMaskTexture.Name}_{finMaterial.Name}";
        (_, IScalarValue maskAlpha)
            = finMaterial.AddTextureSourceColorAlpha(finMaskTexture);
        outputColorAlpha.alpha
            = equations.ScalarOps.Multiply(outputColorAlpha.alpha, maskAlpha);
      }

      equations.SetOutputColorAlpha(outputColorAlpha);

      if (flags.HasFlag(GeoLayerFlags.ENVIRONMENT_MAP)) {
        finMaterial.Name += "_env";
      }

      if (flags.HasFlag(GeoLayerFlags.UV_SCROLL)) {
        finMaterial.Name += "_scroll";
      }

      if (flags.HasFlag(GeoLayerFlags.SUBTRACTIVE)) {
        finMaterial.Name += "_subtract";
        // Shadows: white darkens the surface underneath, black leaves it
        // untouched. The destination alpha is preserved, since
        // subtracting from it would punch holes in the framebuffer's
        // alpha.
        finMaterial.SetBlendingSeparate(BlendEquation.REVERSE_SUBTRACT,
                                        BlendFactor.ONE,
                                        BlendFactor.ONE,
                                        BlendEquation.ADD,
                                        BlendFactor.ZERO,
                                        BlendFactor.ONE,
                                        LogicOp.UNDEFINED);
        finMaterial.DepthMode = DepthMode.READ_ONLY;
      } else if (flags.HasFlag(GeoLayerFlags.ADDITIVE)) {
        finMaterial.Name += "_add";
        // The viewer composites the framebuffer over a checkerboard using its
        // alpha, so the destination alpha is preserved; otherwise these would
        // undo the coverage of any translucent surfaces drawn before them.
        finMaterial.SetBlendingSeparate(BlendEquation.ADD,
                                        BlendFactor.SRC_ALPHA,
                                        BlendFactor.ONE,
                                        BlendEquation.ADD,
                                        BlendFactor.ZERO,
                                        BlendFactor.ONE,
                                        LogicOp.UNDEFINED);
        finMaterial.CullingMode = CullingMode.SHOW_BOTH;
        // Additive blending is order-independent, so these don't need to
        // write depth (which would otherwise occlude each other).
        finMaterial.DepthMode = DepthMode.READ_ONLY;
      } else if (flags.HasFlag(GeoLayerFlags.ALPHA_BLEND)) {
        finMaterial.Name += "_blend";
        // Alpha uses the standard "over" operator (a + (1 - a) * dst), so
        // drawing over opaque geometry keeps the framebuffer opaque. Blending
        // alpha by SRC_ALPHA would lower it to a² + (1 - a), which lets the
        // viewer's checkerboard background show through the glass.
        finMaterial.SetBlendingSeparate(BlendEquation.ADD,
                                        BlendFactor.SRC_ALPHA,
                                        BlendFactor.ONE_MINUS_SRC_ALPHA,
                                        BlendEquation.ADD,
                                        BlendFactor.ONE,
                                        BlendFactor.ONE_MINUS_SRC_ALPHA,
                                        LogicOp.UNDEFINED);
        // Alpha-tested surfaces (foliage) discard their transparent parts, so
        // they can still write depth without occluding what's behind them.
        if (!flags.HasFlag(GeoLayerFlags.ALPHA_TEST)) {
          finMaterial.DepthMode = DepthMode.READ_ONLY;
        }
      } else {
        // Without any blending flags, layers are drawn opaque even if their
        // texture has alpha. Otherwise they'd be treated as transparent and
        // drawn after (and over) actual transparent surfaces like glass.
        // Alpha testing still discards pixels, since that's independent of
        // blending.
        finMaterial.SetBlending(BlendEquation.ADD,
                                BlendFactor.ONE,
                                BlendFactor.ZERO,
                                LogicOp.UNDEFINED);
      }

      if (flags.HasFlag(GeoLayerFlags.ALPHA_TEST)) {
        finMaterial.Name += "_test";
        finMaterial.SetDefaultAlphaCompare(TransparencyType.MASK);
      }

      return finMaterial;
    });

    // The game is left-handed, so the Z axis is flipped to convert it.
    IBone finRootBone = finModel.Skeleton.Root.AddRoot(0, 0, 0);
    finRootBone.Name = geoFile.NameWithoutExtension.ToString();
    finRootBone.Transform.SetScale(1, 1, -1);

    // Some GEOs (pause menu maps, wizard cards, fonts, etc.) only contain a
    // texture, so a plane is generated to display it. Loading screens (*LS)
    // also contain a loading bar and logo, but only the background (the first
    // texture) is shown.
    bool isLoadingScreen
        = geoFile.NameWithoutExtension.EndsWith("LS", StringComparison.Ordinal);
    if (geoMeshes.Count == 0 &&
        (geo.Textures.Length == 1 ||
         (isLoadingScreen && geo.Textures.Length > 0))) {
      AddTexturePlane_(
          finModel,
          finRootBone,
          geo.Textures[0],
          lazyFinMaterials[new MaterialKey(0,
                                           -1,
                                           GeoLayerFlags.ALPHA_BLEND |
                                           GeoLayerFlags.DOUBLE_SIDED,
                                           false)]);
      return finModel;
    }

    if (geo.LayoutObjects.Length > 0) {
      foreach (GeoLayoutObject geoLayoutObject in geo.LayoutObjects) {
        if (!geoMeshesByIndex.TryGetValue(geoLayoutObject.MeshIndex,
                                          out GeoMesh? geoMesh)) {
          continue;
        }

        IBone finBone = finRootBone.AddChild(geoLayoutObject.Position);
        finBone.Name = geoMesh.Name;
        finBone.Transform.SetRotation(
            QuaternionUtil.CreateZyxRadians(-geoLayoutObject.RotationRadians));
        finBone.Transform.SetScale(geoLayoutObject.Scale);

        AddMesh_(finModel, finBone, geoMesh, lazyFinMaterials);
      }
    } else {
      foreach (GeoMesh? geoMesh in geoMeshes.OrderBy(m => m.Index)) {
        AddMesh_(finModel, finRootBone, geoMesh, lazyFinMaterials);
      }
    }

    return finModel;
  }

  private static void AddMesh_(
      ModelImpl finModel,
      IReadOnlyBone finBone,
      GeoMesh geoMesh,
      LazyDictionary<MaterialKey, IMaterial?> lazyFinMaterials) {
    if (geoMesh.Strips.Length == 0) {
      return;
    }

    ISkin<NormalTangentMultiColorMultiUvVertexImpl> finSkin = finModel.Skin;
    IBoneWeights finBoneWeights
        = finSkin.GetOrCreateBoneWeights(VertexSpace.RELATIVE_TO_BONE,
                                         finBone);

    Vector3[] geoPositions = geoMesh.Positions;
    Vector2[] geoTexcoords = geoMesh.Texcoords;
    Vector3[]? geoNormals = geoMesh.Normals;
    byte[]? geoColors = geoMesh.Colors;

    // Strips index positions and texcoords separately, so each unique
    // combination becomes one vertex. Colors are stored per texcoord of the
    // first texture layer, which is also used for the first UV channel. The
    // second UV channel is only used by terrain blends.
    LazyDictionary<(ushort positionIndex, ushort texcoordIndex0, ushort texcoordIndex1), IReadOnlyVertex> finVerticesByKey
        = new(key => {
          (ushort positionIndex, ushort texcoordIndex0, ushort texcoordIndex1)
              = key;

          NormalTangentMultiColorMultiUvVertexImpl finVertex = finSkin.AddVertex(geoPositions[positionIndex]);
          finVertex.SetBoneWeights(finBoneWeights);

          if (geoNormals != null) {
            finVertex.SetLocalNormal(geoNormals[positionIndex]);
          }

          finVertex.SetUv(0, GetTexcoord_(geoTexcoords, texcoordIndex0));
          finVertex.SetUv(1, GetTexcoord_(geoTexcoords, texcoordIndex1));

          if (geoColors != null &&
              (texcoordIndex0 + 1) * 4 <= geoColors.Length) {
            int colorOffset = texcoordIndex0 * 4;
            finVertex.SetColorBytes(geoColors[colorOffset + 0],
                                    geoColors[colorOffset + 1],
                                    geoColors[colorOffset + 2],
                                    geoColors[colorOffset + 3]);
          }

          return finVertex;
        });

    bool hasVertexColors = geoColors != null;
    // Overlays are kept in separate primitives from regular geometry with the
    // same material, since they're drawn at a different priority.
    Dictionary<(MaterialKey, byte overlayPass), List<IReadOnlyVertex>>
        finVerticesByPrimitiveKey = [];
    foreach (GeoStrip geoStrip in geoMesh.Strips) {
      GeoStripVertex[] geoStripVertices = geoStrip.Vertices;
      GeoStripLayer[] geoLayers = geoStrip.Layers;
      if (geoLayers.Length == 0) {
        continue;
      }

      // The only strips with multiple layers are terrain blends, where both
      // layers are combined into a single pass: the first layer is a mask
      // that only provides the alpha, and the second layer provides the
      // color, tiled with its own texcoords.
      if (geoLayers.Length > 2) {
        throw new NotSupportedException(
            $"Strips with {geoLayers.Length} layers aren't supported yet!");
      }

      GeoLayerFlags flags = 0;
      foreach (GeoStripLayer geoLayer in geoLayers) {
        flags |= geoLayer.Flags & SUPPORTED_LAYER_FLAGS_;
      }

      MaterialKey materialKey
          = new(geoLayers[^1].TextureIndex,
                geoLayers.Length == 2 ? geoLayers[0].TextureIndex : -1,
                flags,
                hasVertexColors);
      (MaterialKey, byte) primitiveKey = (materialKey, geoStrip.OverlayPass);
      if (!finVerticesByPrimitiveKey.TryGetValue(
              primitiveKey,
              out List<IReadOnlyVertex>? finVertices)) {
        finVerticesByPrimitiveKey[primitiveKey] = finVertices = [];
      }

      for (int k = 0; k < geoStripVertices.Length - 2; ++k) {
        GeoStripVertex v0 = geoStripVertices[k];
        GeoStripVertex v1 = geoStripVertices[k + 1];
        GeoStripVertex v2 = geoStripVertices[k + 2];

        // Skips degenerate triangles.
        if (v0.PositionIndex == v1.PositionIndex ||
            v1.PositionIndex == v2.PositionIndex ||
            v0.PositionIndex == v2.PositionIndex) {
          continue;
        }

        if (k % 2 == 1) {
          (v1, v2) = (v2, v1);
        }

        foreach (GeoStripVertex v in (ReadOnlySpan<GeoStripVertex>) [v0, v1, v2]) {
          finVertices.Add(finVerticesByKey[(v.PositionIndex,
                                            v.TexcoordIndices[0],
                                            v.TexcoordIndices[^1])]);
        }
      }
    }

    IMesh finMesh = finSkin.AddMesh();
    finMesh.Name = geoMesh.Name;

    foreach (((MaterialKey materialKey, byte overlayPass),
              List<IReadOnlyVertex>? finVertices)
             in finVerticesByPrimitiveKey) {
      if (finVertices.Count == 0) {
        continue;
      }

      IPrimitive finPrimitive = finMesh.AddTriangles(finVertices)
                                .SetVertexOrder(VertexOrder.CLOCKWISE)
                                .SetInversePriority(
                                    GetInversePriority_(materialKey.Flags,
                                      overlayPass));

      IMaterial? finMaterial = lazyFinMaterials[materialKey];
      if (finMaterial != null) {
        finPrimitive.SetMaterial(finMaterial);
      }
    }
  }

  /// <summary>
  ///   Adds an upright plane the size of the texture in pixels, with its
  ///   bottom edge centered on the origin.
  /// </summary>
  private static void AddTexturePlane_(ModelImpl finModel,
                                       IReadOnlyBone finBone,
                                       GeoTexture geoTexture,
                                       IMaterial? finMaterial) {
    ISkin<NormalTangentMultiColorMultiUvVertexImpl> finSkin = finModel.Skin;
    IBoneWeights finBoneWeights
        = finSkin.GetOrCreateBoneWeights(VertexSpace.RELATIVE_TO_BONE,
                                         finBone);

    float scale = 0.01f;
    float halfWidth = geoTexture.Width / 2f * scale;
    float height = geoTexture.Height * scale;

    IReadOnlyVertex AddVertex(float x, float y, float u, float v) {
      NormalTangentMultiColorMultiUvVertexImpl finVertex
          = finSkin.AddVertex(new Vector3(x, y, 0));
      finVertex.SetBoneWeights(finBoneWeights);
      finVertex.SetLocalNormal(new Vector3(0, 0, -1));
      finVertex.SetUv(0, new Vector2(u, v));
      return finVertex;
    }

    IMesh finMesh = finSkin.AddMesh();
    finMesh.Name = geoTexture.Name;
    finMesh.AddQuads(AddVertex(-halfWidth, height, 0, 0),
                     AddVertex(halfWidth, height, 1, 0),
                     AddVertex(halfWidth, 0, 1, 1),
                     AddVertex(-halfWidth, 0, 0, 1))
           .SetMaterial(finMaterial);
  }

  private static Vector2 GetTexcoord_(Vector2[] geoTexcoords,
                                      ushort texcoordIndex)
    => texcoordIndex < geoTexcoords.Length
        ? geoTexcoords[texcoordIndex]
        : Vector2.Zero;

  /// <summary>
  ///   Transparent primitives within a model aren't depth-sorted, so they're
  ///   instead drawn in groups: overlays first (in pass order), since they're
  ///   part of the surfaces underneath them; then free-standing
  ///   alpha-blended surfaces like glass; then free-standing additive
  ///   glows/lightbeams last, since those look roughly correct regardless of
  ///   what they're drawn over.
  /// </summary>
  private static uint GetInversePriority_(GeoLayerFlags flags,
                                          byte overlayPass) {
    if (overlayPass > 0) {
      return (uint) overlayPass - 1;
    }

    if (flags.HasFlag(GeoLayerFlags.SUBTRACTIVE)) {
      return 0;
    }

    if (flags.HasFlag(GeoLayerFlags.ADDITIVE)) {
      return 0x101;
    }

    return flags.HasFlag(GeoLayerFlags.ALPHA_BLEND) ? 0x100u : 0;
  }
}
