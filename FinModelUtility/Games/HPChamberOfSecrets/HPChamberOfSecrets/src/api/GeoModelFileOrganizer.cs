using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

using fin.io;

using hpcos.schema.geo;

namespace hpcos.src.api;

public static class GeoModelFileOrganizer {
  // Categories for organizing models
  private static readonly string CATEGORY_UNCATEGORIZED = "uncategorized";

  private static readonly string CATEGORY_CHARACTERS = "characters";
  private static readonly string CATEGORY_FX = "fx";
  private static readonly string CATEGORY_LOCATIONS = "locations";
  private static readonly string CATEGORY_PROPS = "props";
  private static readonly string CATEGORY_UI = "ui";

  private static readonly string CATEGORY_CHARACTERS_CREATURES = CATEGORY_CHARACTERS + "/creatures";
  private static readonly string CATEGORY_CHARACTERS_HARRY = CATEGORY_CHARACTERS + "/harry";
  private static readonly string CATEGORY_CHARACTERS_NPCS = CATEGORY_CHARACTERS + "/npcs";

  private static readonly string CATEGORY_LOCATIONS_BURROW = CATEGORY_LOCATIONS + "/burrow";
  private static readonly string CATEGORY_LOCATIONS_DIAGON_ALLEY = CATEGORY_LOCATIONS + "/diagon_alley";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_DUNGEONS = CATEGORY_LOCATIONS + "/hogwarts/dungeons";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_0TH_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/0th_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/1st_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/2nd_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/3rd_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/4th_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_5TH_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/5th_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/6th_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR = CATEGORY_LOCATIONS + "/hogwarts/7th_floor";
  private static readonly string CATEGORY_LOCATIONS_HOGWARTS_GROUNDS = CATEGORY_LOCATIONS + "/hogwarts/grounds";

  private static readonly string CATEGORY_UI_MAPS = CATEGORY_UI + "/maps";
  private static readonly string CATEGORY_UI_WIZARD_CARDS = CATEGORY_UI + "/wizard_cards";
  private static readonly string CATEGORY_UI_INVENTORY = CATEGORY_UI + "/inventory";
  private static readonly string CATEGORY_UI_LOADING_SCREENS = CATEGORY_UI + "/loading_screens";



  // Determines where a .geo file should live in the extract folder.
  public static string GetCategory(string nameWithoutExtension) {
    return nameWithoutExtension switch {
      // Creatures
      "Aragog_Spider" => CATEGORY_CHARACTERS_CREATURES,
      "AviforsBird" => CATEGORY_CHARACTERS_CREATURES,
      "Basilisk" => CATEGORY_CHARACTERS_CREATURES,
      "Billywig" => CATEGORY_CHARACTERS_CREATURES,
      "Bookcase" => CATEGORY_CHARACTERS_CREATURES,
      "Bowtruckle" => CATEGORY_CHARACTERS_CREATURES,
      "CagedImp" => CATEGORY_CHARACTERS_CREATURES,
      "ChocFrog" => CATEGORY_CHARACTERS_CREATURES,
      "Fawkes" => CATEGORY_CHARACTERS_CREATURES,
      "FireCrab" => CATEGORY_CHARACTERS_CREATURES,
      "Gargoyle" => CATEGORY_CHARACTERS_CREATURES,
      "GloryHand" => CATEGORY_CHARACTERS_CREATURES,
      "GloryHole" => CATEGORY_CHARACTERS_CREATURES,
      "Gnome" => CATEGORY_CHARACTERS_CREATURES,
      "Gytrash" => CATEGORY_CHARACTERS_CREATURES,
      "Hedwig" => CATEGORY_CHARACTERS_CREATURES,
      "Imp" => CATEGORY_CHARACTERS_CREATURES,
      "MediumSpider" => CATEGORY_CHARACTERS_CREATURES,
      "MMPets" => CATEGORY_CHARACTERS_CREATURES,
      "MorphGhosts" => CATEGORY_CHARACTERS_CREATURES,
      "MrsNorris" => CATEGORY_CHARACTERS_CREATURES,
      "Puffapod" => CATEGORY_CHARACTERS_CREATURES,
      "ScreamingBook" => CATEGORY_CHARACTERS_CREATURES,
      "Small_Spider" => CATEGORY_CHARACTERS_CREATURES,
      "Spider" => CATEGORY_CHARACTERS_CREATURES,
      "SqCaged" => CATEGORY_CHARACTERS_CREATURES,
      "Tentacular" => CATEGORY_CHARACTERS_CREATURES,
      "Trevor" => CATEGORY_CHARACTERS_CREATURES,
      "washer" => CATEGORY_CHARACTERS_CREATURES,
      "WhompingImps" => CATEGORY_CHARACTERS_CREATURES,
      "Yorik" => CATEGORY_CHARACTERS_CREATURES,

      // Harry
      "harry" => CATEGORY_CHARACTERS_HARRY,
      "HarryGYL" => CATEGORY_CHARACTERS_HARRY,
      "HaryHog" => CATEGORY_CHARACTERS_HARRY,
      "HaryQuid" => CATEGORY_CHARACTERS_HARRY,

      // NPCs
      "BloodyBaron" => CATEGORY_CHARACTERS_NPCS,
      "Borgin" => CATEGORY_CHARACTERS_NPCS,
      "BurChars" => CATEGORY_CHARACTERS_NPCS,
      "COS_Ginny" => CATEGORY_CHARACTERS_NPCS,
      "Crabbe" => CATEGORY_CHARACTERS_NPCS,
      "DAWizard" => CATEGORY_CHARACTERS_NPCS,
      "Dedalus" => CATEGORY_CHARACTERS_NPCS,
      "DiagGeorge" => CATEGORY_CHARACTERS_NPCS,
      "DiagGinny" => CATEGORY_CHARACTERS_NPCS,
      "DiagMolly" => CATEGORY_CHARACTERS_NPCS,
      "DiagRon" => CATEGORY_CHARACTERS_NPCS,
      "Doris" => CATEGORY_CHARACTERS_NPCS,
      "DuelMALF" => CATEGORY_CHARACTERS_NPCS,
      "Dumble" => CATEGORY_CHARACTERS_NPCS,
      "Dumble190" => CATEGORY_CHARACTERS_NPCS,
      "EthnicMalePupil" => CATEGORY_CHARACTERS_NPCS,
      "EthnicMalePupilLP" => CATEGORY_CHARACTERS_NPCS,
      "FblotWiz" => CATEGORY_CHARACTERS_NPCS,
      "FemaleGhost" => CATEGORY_CHARACTERS_NPCS,
      "FemalePrefectSkin" => CATEGORY_CHARACTERS_NPCS,
      "FemalePupilSkin" => CATEGORY_CHARACTERS_NPCS,
      "FemalePupilSkinLP" => CATEGORY_CHARACTERS_NPCS,
      "Flitwick" => CATEGORY_CHARACTERS_NPCS,
      "George" => CATEGORY_CHARACTERS_NPCS,
      "Ginny" => CATEGORY_CHARACTERS_NPCS,
      "GJWizard" => CATEGORY_CHARACTERS_NPCS,
      "Goblin" => CATEGORY_CHARACTERS_NPCS,
      "Goyle" => CATEGORY_CHARACTERS_NPCS,
      "hagrid" => CATEGORY_CHARACTERS_NPCS,
      "Hermione" => CATEGORY_CHARACTERS_NPCS,
      "Hooch" => CATEGORY_CHARACTERS_NPCS,
      "JacknJill" => CATEGORY_CHARACTERS_NPCS,
      "LCWitch" => CATEGORY_CHARACTERS_NPCS,
      "LCWizard" => CATEGORY_CHARACTERS_NPCS,
      "LeakyHagrid" => CATEGORY_CHARACTERS_NPCS,
      "Lockhart" => CATEGORY_CHARACTERS_NPCS,
      "Lucius" => CATEGORY_CHARACTERS_NPCS,
      "MalePrefectA1" => CATEGORY_CHARACTERS_NPCS,
      "MalePrefectA2" => CATEGORY_CHARACTERS_NPCS,
      "MalePrefectA3" => CATEGORY_CHARACTERS_NPCS,
      "MalePrefectSkin" => CATEGORY_CHARACTERS_NPCS,
      "Malfoy" => CATEGORY_CHARACTERS_NPCS,
      "McGonagall" => CATEGORY_CHARACTERS_NPCS,
      "MenagWiz" => CATEGORY_CHARACTERS_NPCS,
      "MoaningMyrtle" => CATEGORY_CHARACTERS_NPCS,
      "Molly" => CATEGORY_CHARACTERS_NPCS,
      "MrMulpep" => CATEGORY_CHARACTERS_NPCS,
      "Neville" => CATEGORY_CHARACTERS_NPCS,
      "Nick" => CATEGORY_CHARACTERS_NPCS,
      "Percy" => CATEGORY_CHARACTERS_NPCS,
      "quidmalfoy" => CATEGORY_CHARACTERS_NPCS,
      "QuidPlayers" => CATEGORY_CHARACTERS_NPCS,
      "QuidPlayersF" => CATEGORY_CHARACTERS_NPCS,
      "racersF" => CATEGORY_CHARACTERS_NPCS,
      "racersM" => CATEGORY_CHARACTERS_NPCS,
      "Ron" => CATEGORY_CHARACTERS_NPCS,
      "SeatedFemalePupil" => CATEGORY_CHARACTERS_NPCS,
      "SeatedMalePupil" => CATEGORY_CHARACTERS_NPCS,
      "Snape" => CATEGORY_CHARACTERS_NPCS,
      "Sprout" => CATEGORY_CHARACTERS_NPCS,
      "Tom" => CATEGORY_CHARACTERS_NPCS,
      "TomRiddle" => CATEGORY_CHARACTERS_NPCS,
      "Turner" => CATEGORY_CHARACTERS_NPCS,
      "Wood" => CATEGORY_CHARACTERS_NPCS,
      "Friar" => CATEGORY_CHARACTERS_NPCS,

      // FX
      "DuelDude" => CATEGORY_FX,
      "GhostBox" => CATEGORY_FX,
      "Harry_FX" => CATEGORY_FX,
      "HarryALH" => CATEGORY_FX,
      "HarryAVI" => CATEGORY_FX,
      "HarryDIF" => CATEGORY_FX,
      "HarryEXP" => CATEGORY_FX,
      "HarryFLP" => CATEGORY_FX,
      "HarryINC" => CATEGORY_FX,
      "HarryLUM" => CATEGORY_FX,
      "HarrySKG" => CATEGORY_FX,
      "rings" => CATEGORY_FX,

      // Burrow
      "Burrow" => CATEGORY_LOCATIONS_BURROW,

      // Diagon Alley
      "B_Burkes" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "DiagonAlly" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "F_Blotts" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "G_Japes" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "Gringott" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "LeakyC" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "MagMenag" => CATEGORY_LOCATIONS_DIAGON_ALLEY,
      "Mulpepp" => CATEGORY_LOCATIONS_DIAGON_ALLEY,

      // Hogwarts Dungeons
      "StealthDun" => CATEGORY_LOCATIONS_HOGWARTS_DUNGEONS,

      // Hogwarts Ground Floor
      "EntHall" => CATEGORY_LOCATIONS_HOGWARTS_0TH_FLOOR,
      "EntHall_Night" => CATEGORY_LOCATIONS_HOGWARTS_0TH_FLOOR,
      "GrtHall" => CATEGORY_LOCATIONS_HOGWARTS_0TH_FLOOR,
      "StaircasGeom" => CATEGORY_LOCATIONS_HOGWARTS_0TH_FLOOR,
      "StaircasGeom_Night" => CATEGORY_LOCATIONS_HOGWARTS_0TH_FLOOR,

      // Hogwarts Grounds
      "AraArena" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "AraForest" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "AraLair" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "Herbology" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "Herbology_Night" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "Hogwarts" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "Hogwarts_Night" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "Quiddtch" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "WGytrash" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "Whomping" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "WImpArea" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,
      "WWiggenweld" => CATEGORY_LOCATIONS_HOGWARTS_GROUNDS,

      // Hogwarts 1st Floor
      "Avivors" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "Landing1" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "Landing1_Night" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "Landing1A" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "landing1a_night" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "Landing1B" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "landing1b_night" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "Landing1C" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,
      "landing1c_night" => CATEGORY_LOCATIONS_HOGWARTS_1ST_FLOOR,

      // Hogwarts 2nd Floor
      "ChambOfS" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "ChambOfS_Night" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "Incendio" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "Landing2" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "Landing2_Night" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "Landing2A" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "landing2a_night" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "Landing2B" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "landing2b_night" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "Library" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "LibrPotn" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "LibrPotn_Night" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "StealthLib" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,
      "StealthLib_Night" => CATEGORY_LOCATIONS_HOGWARTS_2ND_FLOOR,

      // Hogwarts 3rd Floor
      "Dada" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "DDoffice" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "DDoffice_Night" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "Expelmus" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "Landing3" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "Landing3_Night" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "Landing3A" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "landing3a_night" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "Landing3B" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "landing3b_night" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "Landing3C" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,
      "landing3c_night" => CATEGORY_LOCATIONS_HOGWARTS_3RD_FLOOR,

      // Hogwarts 4th Floor
      "Landing4" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "Landing4_Night" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "Landing4A" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "landing4a_night" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "Landing4B" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "landing4b_night" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "Landing4C" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "landing4c_night" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "Landing4D" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,
      "landing4d_night" => CATEGORY_LOCATIONS_HOGWARTS_4TH_FLOOR,

      // Hogwarts 5th Floor
      "Landing5" => CATEGORY_LOCATIONS_HOGWARTS_5TH_FLOOR,
      "Landing5_Night" => CATEGORY_LOCATIONS_HOGWARTS_5TH_FLOOR,
      "Landing5A" => CATEGORY_LOCATIONS_HOGWARTS_5TH_FLOOR,
      "landing5a_night" => CATEGORY_LOCATIONS_HOGWARTS_5TH_FLOOR,

      // Hogwarts 6th Floor
      "Landing6" => CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR,
      "Landing6_Night" => CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR,
      "Landing6A" => CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR,
      "landing6a_night" => CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR,
      "Landing6B" => CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR,
      "landing6b_night" => CATEGORY_LOCATIONS_HOGWARTS_6TH_FLOOR,

      // Hogwarts 7th Floor
      "GriffRoom" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "GriffRoom_Night" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "Landing7" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "Landing7_Night" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "Landing7A" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "landing7a_night" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "Landing7B" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "landing7b_night" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "Landing7C" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,
      "landing7c_night" => CATEGORY_LOCATIONS_HOGWARTS_7TH_FLOOR,

      // Props
      "anglia" => CATEGORY_PROPS,
      "book" => CATEGORY_PROPS,
      "Cages" => CATEGORY_PROPS,
      "Cask" => CATEGORY_PROPS,
      "COS_Items" => CATEGORY_PROPS,
      "crate" => CATEGORY_PROPS,
      "Cupboard" => CATEGORY_PROPS,
      "DADAbits" => CATEGORY_PROPS,
      "Desk" => CATEGORY_PROPS,
      "DiffindoRope" => CATEGORY_PROPS,
      "doorskins" => CATEGORY_PROPS,
      "doubledoorskins" => CATEGORY_PROPS,
      "Dungbomb" => CATEGORY_PROPS,
      "FordAng" => CATEGORY_PROPS,
      "Gates" => CATEGORY_PROPS,
      "GlassJar" => CATEGORY_PROPS,
      "gntargets" => CATEGORY_PROPS,
      "Health" => CATEGORY_PROPS,
      "Herbprops" => CATEGORY_PROPS,
      "ImpCage" => CATEGORY_PROPS,
      "Nimbus" => CATEGORY_PROPS,
      "NormalBook" => CATEGORY_PROPS,
      "Skurge" => CATEGORY_PROPS,
      "snitch" => CATEGORY_PROPS,
      "Staircas" => CATEGORY_PROPS,
      "Staircas_Night" => CATEGORY_PROPS,
      "Tapestry" => CATEGORY_PROPS,
      "Trunk" => CATEGORY_PROPS,

      // Inventory
      "inv_records" => CATEGORY_UNCATEGORIZED,
      _ when nameWithoutExtension.StartsWith("inv", StringComparison.Ordinal) => CATEGORY_UI_INVENTORY,

      // Loading Screens
      _ when nameWithoutExtension.EndsWith("LS", StringComparison.Ordinal) => CATEGORY_UI_LOADING_SCREENS,

      // Maps
      _ when nameWithoutExtension.StartsWith("Map_", StringComparison.Ordinal) => CATEGORY_UI_MAPS,

      // Wizard Cards
      _ when nameWithoutExtension.StartsWith("Wizardcard", StringComparison.Ordinal) => CATEGORY_UI_WIZARD_CARDS,

      // Unsupported, development only, or cut content
      _ => CATEGORY_UNCATEGORIZED
    };
  }
}
