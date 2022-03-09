using System;
using System.Collections.Generic;
using HarmonyLib;
using Amplitude;
using System.IO;
using Newtonsoft.Json;
using Amplitude.Mercury.Terrain;
using static Amplitude.Mercury.Runtime.RuntimeManager;
using Amplitude.Framework.Asset;
using UnityEngine;

namespace Gedemon.TrueCultureLocation
{
    public class TestClass
    {
        public List<int> MapTerritoryHash = new List<int> { -819807177, -288044546 };
        public IDictionary<string, List<int>> MinorFactionTerritories = new Dictionary<string, List<int>>
					{
                        { "IndependentPeople_Era1_Peaceful_Akkadians",      new List<int>() { 142, 123 } },
						{ "IndependentPeople_Era1_Peaceful_Elamites",       new List<int>() { 243 } },
						{ "IndependentPeople_Era1_Peaceful_Noks",           new List<int>() { 220 } },
					};
        public IDictionary<int, Hexagon.OffsetCoords> ExtraPositions = new Dictionary<int, Hexagon.OffsetCoords>
				{
                    { 0, new Hexagon.OffsetCoords(32, 41)},
					{ 1, new Hexagon.OffsetCoords(40, 51)},
					{ 2, new Hexagon.OffsetCoords(56, 55)}, 
				};
        public IDictionary<int, string> ContinentNames = new Dictionary<int, string>
                {
                    { 0, "Oceans"},
                    { 1, "Americas"},
                    { 2, "Eurasiafrica"},
                };
        public List<string> NoCapital = new List<string> { "Civilization_Era1_Assyria", "Civilization_Era1_HarappanCivilization"};

    }
    public class CityPosition
    {
        public int TerritoryIndex { get; set; }
        public string Name { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Size { get; set; }
        public Hexagon.OffsetCoords Position { get; set; }
    }
    public class MapTCL
    {
        public int LoadOrder { get; set; }
        public List<int> MapTerritoryHash { get; set; }
        public IDictionary<string, List<int>> MajorEmpireTerritories { get; set; }
        public IDictionary<string, List<int>> MajorEmpireCoreTerritories { get; set; }
        public IDictionary<string, List<int>> MinorFactionTerritories { get; set; }
        public IDictionary<int, string> ContinentNames { get; set; }
        public IDictionary<int, string> TerritoryNames { get; set; }
        public List<CityPosition> CityMap { get; set; }
        public IDictionary<int, Hexagon.OffsetCoords> ExtraPositions { get; set; }
        public IDictionary<int, Hexagon.OffsetCoords> ExtraPositionsNewWorld { get; set; }
        public List<string> NoCapital { get; set; }
        public List<string> NomadCultures { get; set; }

    }

    public class TerritoriesLoadingList
    {
        public int loadOrder;
        public List<int> territories;
    }

    public class NamesLoadingString
    {
        public int loadOrder;
        public string name;
    }

    public class PositionLoadingCoords
    {
        public int loadOrder;
        public Hexagon.OffsetCoords position;
    }
    public class CityMapLoading
    {
        public int loadOrder;
        public CityPosition cityPosition;
    }

    class ModLoading
    {
        static IDictionary<string, IList<MapTCL>> listTCLMods = new Dictionary<string, IList<MapTCL>>();

        static IDictionary<string, TerritoriesLoadingList> MajorEmpireTerritoriesPreList = new Dictionary<string, TerritoriesLoadingList>();
        static IDictionary<string, TerritoriesLoadingList> MajorEmpireCoreTerritoriesPreList = new Dictionary<string, TerritoriesLoadingList>();
        static IDictionary<string, TerritoriesLoadingList> MinorFactionTerritoriesPreList = new Dictionary<string, TerritoriesLoadingList>();

        static IDictionary<int, NamesLoadingString> TerritoryNamesPreList = new Dictionary<int, NamesLoadingString>();
        static IDictionary<int, NamesLoadingString> ContinentNamesPreList = new Dictionary<int, NamesLoadingString>();

        static IDictionary<string, CityMapLoading> CityMapPreList = new Dictionary<string, CityMapLoading>();

        static IDictionary<int, PositionLoadingCoords> ExtraPositionsPreList = new Dictionary<int, PositionLoadingCoords>();
        static IDictionary<int, PositionLoadingCoords> ExtraPositionsNewWorldPreList = new Dictionary<int, PositionLoadingCoords>();

        public static void AddTCLfromJSON(string text, string provider)
        {
            if (!listTCLMods.ContainsKey(provider))
            {
				/*
				TestClass testClass = new TestClass();
				string json = JsonConvert.SerializeObject(testClass, Formatting.Indented);
				Diagnostics.LogError($"[Gedemon] serialized testClass = {json}");
				//*/

				Diagnostics.LogWarning($"[Gedemon] deserialize modded TCL from {provider}");
                IList<MapTCL> moddedTCL = JsonConvert.DeserializeObject<List<MapTCL>>(text);
                listTCLMods.Add(provider, moddedTCL);
                /*
                Diagnostics.Log($"[Gedemon] moddedTCL[0].MinorFactionTerritories = {moddedTCL[0].MinorFactionTerritories["IndependentPeople_Era1_Peaceful_SC_Dilmun"][1]}");
                //*/
            }
        }

        public static void AddModdedTCL(IList<MapTCL> moddedTCL, string provider)
        {
            if (!listTCLMods.ContainsKey(provider))
            {
                Diagnostics.LogWarning($"[Gedemon] Add Modded TCL List from: {provider}.");
                listTCLMods.Add(provider, moddedTCL);
            }
            else
            {
                Diagnostics.LogError($"[Gedemon] Error : Trying to add already loaded TCL List from: {provider}");
            }
        }

        public static void RemoveModdedTCL(string provider)
        {
            if (listTCLMods.ContainsKey(provider))
            {
                Diagnostics.LogWarning($"[Gedemon] Remove Modded TCL from: {provider}.");
                listTCLMods.Remove(provider);
            }
        }

        public static void BuildModdedLists()
        {
            Diagnostics.LogError($"[Gedemon] [ModLoading] in BuildModdedLists...");

            string basefolder = Amplitude.Framework.Application.GameDirectory;
            List<string> folders = new List<string> { System.IO.Path.Combine(basefolder, TerrainSave.MapsSubDirectory), System.IO.Path.Combine(basefolder, RuntimeModuleFolders.Public.Name), System.IO.Path.Combine(basefolder, RuntimeModuleFolders.Community.Name) };
            foreach(string path in folders)
            {
                Diagnostics.Log($"[Gedemon] searching for .json files in {path}");
                if (Directory.Exists(path))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    if (directoryInfo.Exists)
                    {
                        Diagnostics.Log($"[Gedemon] searching *TCL.json files in {directoryInfo.FullName}");
                        string searchPatternJSON = "*TCL.json";
                        //SearchOption searchOption = (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        FileInfo[] filesJSON = directoryInfo.GetFiles(searchPatternJSON, SearchOption.AllDirectories);
                        foreach (FileInfo fileInfo in filesJSON)
                        {
                            Diagnostics.LogWarning($"[Gedemon] loading *TCL.json file : ({fileInfo.FullName})");
                            StreamReader stream = fileInfo.OpenText();
                            ModLoading.AddTCLfromJSON(stream.ReadToEnd(), fileInfo.FullName);
                        }

                        Diagnostics.Log($"[Gedemon] searching *.XML files in {directoryInfo.FullName}");
                        string searchPatternXML = "*.XML";

                        FileInfo[] filesXML = directoryInfo.GetFiles(searchPatternXML, SearchOption.AllDirectories);
                        foreach (FileInfo fileInfo in filesXML)
                        {
                            Diagnostics.LogWarning($"[Gedemon] loading *.XML file : ({fileInfo.FullName})");
                            //StreamReader stream = fileInfo.OpenText();
                            DatabaseUtils.LoadXML(fileInfo.FullName, fileInfo.FullName);
                        }
                    }
                }
            }
            //
            //string path = Amplitude.Framework.Application.GameDirectory; // + "\\" + TerrainSave.MapsSubDirectory + "\\";

            foreach (KeyValuePair<string, IList<MapTCL>> kvp in listTCLMods)
            {

                foreach (MapTCL mapTCL in kvp.Value)
                {
                    if (mapTCL.MapTerritoryHash != null)
                    {
                        if (CultureUnlock.HasCurrentMapHash(mapTCL.MapTerritoryHash) && !CultureUnlock.HasGiantEarthMapHash(mapTCL.MapTerritoryHash))
                        {
                            if(CultureUnlock.MapCanUseGiantEarthReference)
                            {
                                Diagnostics.LogWarning($"[Gedemon] [ModLoading] Found modded data from {kvp.Key} with valid MapHash (current = {CultureUnlock.CurrentMapHash}) for a Map that is not the Giant Earth Map, Set MapCanUseGiantEarthReference to false");
                                CultureUnlock.MapCanUseGiantEarthReference = false;
                            }
                        }
                    }
                }

                Diagnostics.LogError($"[Gedemon] [ModLoading] Applying Modded TCL from {kvp.Key}");
                foreach (MapTCL mapTCL in kvp.Value)
                {
                    if (mapTCL.MapTerritoryHash != null)
                    {
                        if (CultureUnlock.HasValidMapHash(mapTCL.MapTerritoryHash))
                        {
                            Diagnostics.Log($"[Gedemon] - Found valid MapHash (current = {CultureUnlock.CurrentMapHash})");

                            if(mapTCL.MajorEmpireTerritories != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found MajorEmpireTerritories");
                                foreach (KeyValuePair <string, List<int>> factionTerritories in mapTCL.MajorEmpireTerritories)
                                {
                                    string factionName = factionTerritories.Key;
                                    if (MajorEmpireTerritoriesPreList.ContainsKey(factionName))
                                    {
                                        if(MajorEmpireTerritoriesPreList[factionName].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - MajorEmpireTerritories : Replacing modded Territory list for {factionName} (old load order = {MajorEmpireTerritoriesPreList[factionName].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            MajorEmpireTerritoriesPreList[factionName].territories = factionTerritories.Value;
                                            MajorEmpireTerritoriesPreList[factionName].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - MajorEmpireTerritories : Ignoring modded Territory list for {factionName}, already set with same or higher load order (current load order = {MajorEmpireTerritoriesPreList[factionName].loadOrder}, file load order = {mapTCL.LoadOrder})");

                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - MajorEmpireTerritories : Adding modded Territory list for {factionName} with load order = {mapTCL.LoadOrder}");
                                        TerritoriesLoadingList listTerritories = new TerritoriesLoadingList();
                                        listTerritories.loadOrder = mapTCL.LoadOrder;
                                        listTerritories.territories = factionTerritories.Value;
                                        MajorEmpireTerritoriesPreList.Add(factionName, listTerritories);
                                    }
                                }
                            }

                            if (mapTCL.MajorEmpireCoreTerritories != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found MajorEmpireCoreTerritories");
                                foreach (KeyValuePair<string, List<int>> factionTerritories in mapTCL.MajorEmpireCoreTerritories)
                                {
                                    string factionName = factionTerritories.Key;
                                    if (MajorEmpireCoreTerritoriesPreList.ContainsKey(factionName))
                                    {
                                        if (MajorEmpireCoreTerritoriesPreList[factionName].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - MajorEmpireCoreTerritories : Replacing modded Territory list for {factionName} (old load order = {MajorEmpireCoreTerritoriesPreList[factionName].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            MajorEmpireCoreTerritoriesPreList[factionName].territories = factionTerritories.Value;
                                            MajorEmpireCoreTerritoriesPreList[factionName].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - MajorEmpireCoreTerritories : Ignoring modded Territory list for {factionName}, already set with same or higher load order (current load order = {MajorEmpireCoreTerritoriesPreList[factionName].loadOrder}, file load order = {mapTCL.LoadOrder})");

                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - MajorEmpireCoreTerritories : Adding modded Territory list for {factionName} with load order = {mapTCL.LoadOrder}");
                                        TerritoriesLoadingList listTerritories = new TerritoriesLoadingList();
                                        listTerritories.loadOrder = mapTCL.LoadOrder;
                                        listTerritories.territories = factionTerritories.Value;
                                        MajorEmpireCoreTerritoriesPreList.Add(factionName, listTerritories);
                                    }
                                }
                            }

                            if (mapTCL.MinorFactionTerritories != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found MinorFactionTerritories");
                                foreach (KeyValuePair<string, List<int>> factionTerritories in mapTCL.MinorFactionTerritories)
                                {
                                    string factionName = factionTerritories.Key;
                                    if (MinorFactionTerritoriesPreList.ContainsKey(factionName))
                                    {
                                        if (MinorFactionTerritoriesPreList[factionName].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - MinorFactionTerritories: Replacing modded Territory list for {factionName} (old load order = {MinorFactionTerritoriesPreList[factionName].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            MinorFactionTerritoriesPreList[factionName].territories = factionTerritories.Value;
                                            MinorFactionTerritoriesPreList[factionName].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - MinorFactionTerritories : Ignoring modded Territory list for {factionName}, already set with same or higher load order (current load order = {MinorFactionTerritoriesPreList[factionName].loadOrder}, file load order = {mapTCL.LoadOrder})");

                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - MinorFactionTerritories: Adding modded Territory list for {factionName} with load order = {mapTCL.LoadOrder}");
                                        TerritoriesLoadingList listTerritories = new TerritoriesLoadingList();
                                        listTerritories.loadOrder = mapTCL.LoadOrder;
                                        listTerritories.territories = factionTerritories.Value;
                                        MinorFactionTerritoriesPreList.Add(factionName, listTerritories);
                                    }
                                }
                            }

                            if (mapTCL.TerritoryNames != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found TerritoryNames");
                                foreach (KeyValuePair<int, string> territoryName in mapTCL.TerritoryNames)
                                {
                                    int index = territoryName.Key;
                                    if (TerritoryNamesPreList.ContainsKey(index))
                                    {
                                        if (TerritoryNamesPreList[index].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - TerritoryNames: Replacing modded name {territoryName.Value} for ID#{index} (old load order = {TerritoryNamesPreList[index].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            TerritoryNamesPreList[index].name = territoryName.Value;
                                            TerritoryNamesPreList[index].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - TerritoryNames : Ignoring modded name {territoryName.Value} for ID#{index}, already set with same or higher load order (current load order = {TerritoryNamesPreList[index].loadOrder}, file load order = {mapTCL.LoadOrder})");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - TerritoryNames: Adding modded name {territoryName.Value} for ID#{index} with load order = {mapTCL.LoadOrder}");
                                        NamesLoadingString loadingString = new NamesLoadingString();
                                        loadingString.loadOrder = mapTCL.LoadOrder;
                                        loadingString.name = territoryName.Value;
                                        TerritoryNamesPreList.Add(index, loadingString);
                                    }
                                }
                            }

                            if (mapTCL.ContinentNames != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found ContinentNames");
                                foreach (KeyValuePair<int, string> continentName in mapTCL.ContinentNames)
                                {
                                    int index = continentName.Key;
                                    if (ContinentNamesPreList.ContainsKey(index))
                                    {
                                        if (ContinentNamesPreList[index].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - ContinentNames: Replacing modded name {continentName.Value} for ID#{index} (old load order = {ContinentNamesPreList[index].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            ContinentNamesPreList[index].name = continentName.Value;
                                            ContinentNamesPreList[index].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - ContinentNames : Ignoring modded name {continentName.Value} for ID#{index}, already set with same or higher load order (current load order = {ContinentNamesPreList[index].loadOrder}, file load order = {mapTCL.LoadOrder})");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - ContinentNames: Adding modded name {continentName.Value} for ID#{index} with load order = {mapTCL.LoadOrder}");
                                        NamesLoadingString loadingString = new NamesLoadingString();
                                        loadingString.loadOrder = mapTCL.LoadOrder;
                                        loadingString.name = continentName.Value;
                                        ContinentNamesPreList.Add(index, loadingString);
                                    }
                                }
                            }

                            if (mapTCL.ExtraPositions != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found ExtraPositions");
                                foreach (KeyValuePair<int, Hexagon.OffsetCoords> position in mapTCL.ExtraPositions)
                                {
                                    int index = position.Key;
                                    if (ExtraPositionsPreList.ContainsKey(index))
                                    {
                                        if (ExtraPositionsPreList[index].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - ExtraPositions: Replacing modded position {position.Value} for ID#{index} (old load order = {ExtraPositionsPreList[index].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            ExtraPositionsPreList[index].position = position.Value;
                                            ExtraPositionsPreList[index].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - ExtraPositions : Ignoring modded position {position.Value} for ID#{index}, already set with same or higher load order (current load order = {ExtraPositionsPreList[index].loadOrder}, file load order = {mapTCL.LoadOrder})");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - ExtraPositions: Adding modded position {position.Value} for ID#{index} with load order = {mapTCL.LoadOrder}");
                                        PositionLoadingCoords loadingPosition = new PositionLoadingCoords();
                                        loadingPosition.loadOrder = mapTCL.LoadOrder;
                                        loadingPosition.position = position.Value;
                                        ExtraPositionsPreList.Add(index, loadingPosition);
                                    }
                                }
                            }

                            if (mapTCL.ExtraPositionsNewWorld != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found ExtraPositionsNewWorld");
                                foreach (KeyValuePair<int, Hexagon.OffsetCoords> position in mapTCL.ExtraPositionsNewWorld)
                                {
                                    int index = position.Key;
                                    if (ExtraPositionsNewWorldPreList.ContainsKey(index))
                                    {
                                        if (ExtraPositionsNewWorldPreList[index].loadOrder < mapTCL.LoadOrder)
                                        {
                                            Diagnostics.Log($"[Gedemon] - ExtraPositionsNewWorld: Replacing modded position {position.Value} for ID#{index} (old load order = {ExtraPositionsNewWorldPreList[index].loadOrder}, new load order = {mapTCL.LoadOrder})");
                                            ExtraPositionsNewWorldPreList[index].position = position.Value;
                                            ExtraPositionsNewWorldPreList[index].loadOrder = mapTCL.LoadOrder;
                                        }
                                        else
                                        {
                                            Diagnostics.Log($"[Gedemon] - ExtraPositionsNewWorld : Ignoring modded position {position.Value} for ID#{index}, already set with same or higher load order (current load order = {ExtraPositionsNewWorldPreList[index].loadOrder}, file load order = {mapTCL.LoadOrder})");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.Log($"[Gedemon] - ExtraPositionsNewWorld: Adding modded position {position.Value} for ID#{index} with load order = {mapTCL.LoadOrder}");
                                        PositionLoadingCoords loadingPosition = new PositionLoadingCoords();
                                        loadingPosition.loadOrder = mapTCL.LoadOrder;
                                        loadingPosition.position = position.Value;
                                        ExtraPositionsNewWorldPreList.Add(index, loadingPosition);
                                    }
                                }
                            }
                                                        
                            if (mapTCL.NoCapital != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found NoCapital list");
                                foreach (string factionName in mapTCL.NoCapital)
                                {
                                    Diagnostics.Log($"[Gedemon] Updating noCapitalTerritory, adding {factionName}");
                                    CultureUnlock.UpdateListNoCapitalTerritory(factionName); // immediate update
                                }
                            }
                            
                            if (mapTCL.NomadCultures != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found NomadCultures list");
                                foreach (string factionName in mapTCL.NomadCultures)
                                {
                                    Diagnostics.Log($"[Gedemon] Updating nomadCultures, adding {factionName}");
                                    CultureUnlock.UpdateListNomads(factionName); // immediate update
                                }
                            }

                            if (mapTCL.CityMap != null)
                            {
                                Diagnostics.Log($"[Gedemon] - Found CityPosition list (num entries = {mapTCL.CityMap.Count})");
                                foreach (CityPosition cityPosition in mapTCL.CityMap)
                                {
                                    //Diagnostics.Log($"[Gedemon] Updating WorldCityMap, adding {cityPosition.Name}");
                                    CityMap.WorldCityMap.Add(cityPosition); // immediate update
                                }
                                Diagnostics.Log($"[Gedemon] Updated WorldCityMap (total num entries = {CityMap.WorldCityMap.Count})");
                            }
                        }
                    }
                    else
                    {
                        Diagnostics.LogError($"[Gedemon] Error: MapTerritoryHash is null");
                    }
                }
            }

            foreach (KeyValuePair<string, TerritoriesLoadingList> factionTerritories in MajorEmpireTerritoriesPreList)
            {
                string factionName = factionTerritories.Key;
                Diagnostics.LogWarning($"[Gedemon] Updating MajorEmpireTerritories for {factionName}");
                CultureUnlock.UpdateListMajorEmpireTerritories(factionName, factionTerritories.Value.territories);
            }

            foreach (KeyValuePair<string, TerritoriesLoadingList> factionTerritories in MajorEmpireCoreTerritoriesPreList)
            {
                string factionName = factionTerritories.Key;
                Diagnostics.LogWarning($"[Gedemon] Updating MajorEmpireCoreTerritories for {factionName}");
                CultureUnlock.UpdateListMajorEmpireCoreTerritories(factionName, factionTerritories.Value.territories);
            }

            foreach (KeyValuePair<string, TerritoriesLoadingList> factionTerritories in MinorFactionTerritoriesPreList)
            {
                string factionName = factionTerritories.Key;
                Diagnostics.LogWarning($"[Gedemon] Updating MinorFactionTerritories for {factionName}");
                CultureUnlock.UpdateListMinorFactionTerritories(factionName, factionTerritories.Value.territories);
            }

            foreach (KeyValuePair<int, NamesLoadingString> kvp in TerritoryNamesPreList)
            {
                Diagnostics.LogWarning($"[Gedemon] Updating TerritoryNames ID#{kvp.Key} = {kvp.Value.name}");
                CultureUnlock.UpdateListTerritoryNames(kvp.Key, kvp.Value.name);
            }

            foreach (KeyValuePair<int, NamesLoadingString> kvp in ContinentNamesPreList)
            {
                Diagnostics.LogWarning($"[Gedemon] Updating ContinentNames ID#{kvp.Key} = {kvp.Value.name}");
                CultureUnlock.UpdateListContinentNames(kvp.Key, kvp.Value.name);
            }

            foreach (KeyValuePair<int, PositionLoadingCoords> kvp in ExtraPositionsPreList)
            {
                Diagnostics.LogWarning($"[Gedemon] Updating ExtraPositions Slot#{kvp.Key} = {kvp.Value.position}");
                CultureUnlock.UpdateListExtraPositions(kvp.Key, kvp.Value.position);
            }

            foreach (KeyValuePair<int, PositionLoadingCoords> kvp in ExtraPositionsNewWorldPreList)
            {
                Diagnostics.LogWarning($"[Gedemon] Updating ExtraPositions (New World) Slot#{kvp.Key} = {kvp.Value.position}");
                CultureUnlock.UpdateListExtraPositionsNewWorld(kvp.Key, kvp.Value.position);
            }

        }


        public static void OnSandboxStarted()
        {
        }
        public static void OnExitSandbox()
        {
            //WorldCityMap.Clear();
            //TerritoryCityMap.Clear();
            //TranslationTable.Clear();

            MajorEmpireTerritoriesPreList.Clear();
            MajorEmpireCoreTerritoriesPreList.Clear();
            MinorFactionTerritoriesPreList.Clear();

            TerritoryNamesPreList.Clear();
            ContinentNamesPreList.Clear();

            CityMapPreList.Clear();

            ExtraPositionsPreList.Clear();
            ExtraPositionsNewWorldPreList.Clear();
        }
    }

    //*
    [HarmonyPatch(typeof(AssetDatabase))]
    public class AssetDatabase_Patch
    {
        [HarmonyPatch("TryMountAssetBundle")]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(uint), typeof(IAssetProvider), typeof(Amplitude.Framework.Asset.AssetBundle.Options) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool TryMountAssetBundle(string providerName, string path, uint assetBundleFlags, Amplitude.Framework.Asset.AssetBundle.Options options)
        {
            if (path.Contains("Community"))
            {
                Diagnostics.LogError($"[Gedemon] [AssetDatabase] TryMountAssetBundle from \\Community\\ : providerName = {providerName}, assetBundleFlags = {assetBundleFlags}, options = {options}");
                Diagnostics.LogError($"[Gedemon] [AssetDatabase] Path = {path}");
                UnityEngine.AssetBundle assetBundle = null;
                bool flag = false;
                try
                {
                    flag = AssetDatabase.TryGetAssetBundleFromPath(path, out assetBundle);
                    if (!flag)
                    {
                        assetBundle = UnityEngine.AssetBundle.LoadFromFile(path);
                    }
                    if (assetBundle != null)
                    {
                        Diagnostics.Log($"[Gedemon] [AssetDatabase] GetAllAssetNames, loaded: {flag}).");
                        string[] assetFiles = assetBundle.GetAllAssetNames();
                        foreach (string assetName in assetFiles)
                        {
                            string lowerCase = assetName.ToLower();
                            if (lowerCase.EndsWith("tcl.json"))
                            {
                                Diagnostics.Log($"[Gedemon] [AssetDatabase] assetBundle contains *tcl.json ({assetName})");
                                TextAsset textAsset = assetBundle.LoadAsset<TextAsset>(assetName);
                                //Diagnostics.LogWarning($"[Gedemon] [RuntimeManager] TCL.json = {textAsset.text}");
                                ModLoading.AddTCLfromJSON(textAsset.text, providerName);
                            }
                            if (lowerCase.EndsWith(".xml"))
                            {
                                Diagnostics.Log($"[Gedemon] [AssetDatabase] assetBundle contains *.xml ({assetName})");
                                TextAsset textAsset = assetBundle.LoadAsset<TextAsset>(assetName);
                                DatabaseUtils.LoadXML(textAsset.text, providerName, inputIsText:true);
                                //Diagnostics.LogWarning($"[Gedemon] [RuntimeManager] TCL.json = {textAsset.text}");
                            }
                        }
                    }
                    else
                    {
                        Diagnostics.LogError($"[Gedemon] [AssetDatabase] Failed to load asset bundle, loaded: {flag}).");
                    }

                }
                catch (Exception exception)
                {
                    Diagnostics.LogException(exception);
                }
                finally
                {
                    if (assetBundle != null && !flag)
                    {
                        assetBundle.Unload(unloadAllLoadedObjects: false);
                        assetBundle = null;
                    }
                }
            }
            return true;
        }

        [HarmonyPatch("UnmountAssetBundle")]
        [HarmonyPatch(new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool UnmountAssetBundle(string providerName)
        {
            ModLoading.RemoveModdedTCL(providerName);
            return true;
        }
    }
    //*/

}
