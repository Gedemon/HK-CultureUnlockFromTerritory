using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Localization;
using Amplitude.Mercury;
using Amplitude.Mercury.Simulation;

namespace Gedemon.TrueCultureLocation
{
    class DatabaseUtils
    {

        #region CityData 
        // to do : create CityMap class file

        public class CityPosition
        {
            public int TerritoryIndex { get; set; }
            public string Name { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
            public int Size { get; set; }
            public Hexagon.OffsetCoords Position { get; set; }
        }

        static List<CityPosition> WorldCityMap = new List<CityPosition>();

        public static IDictionary<int, List<CityPosition>> TerritoryCityMap = new Dictionary<int, List<CityPosition>>();


        public static void BuildTerritoryCityMap(World currentWorld)
        {
            foreach(CityPosition cityPosition in WorldCityMap)
            {
                WorldPosition position = new WorldPosition(cityPosition.Column, cityPosition.Row);
                int territoryIndex = currentWorld.TileInfo.Data[position.ToTileIndex()].TerritoryIndex;

                if(TerritoryCityMap.TryGetValue(territoryIndex, out List<CityPosition> cityList))
                {
                    cityList.Add(cityPosition);
                    //TerritoryCityMap[territoryIndex] = cityList;
                }
                else
                {
                    TerritoryCityMap.Add(territoryIndex, new List<CityPosition> { cityPosition });
                }

            }

            foreach(KeyValuePair<int, List<CityPosition>> kvp in TerritoryCityMap)
            {
                int territoryIndex = kvp.Key;
                List<CityPosition> cityList = kvp.Value;
                if(cityList.Count > 0)
                {
                    Diagnostics.LogWarning($"[Gedemon] [BuildTerritoryCityMap] City list for Territory #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)})");
                    foreach(CityPosition cityPosition in cityList)
                    {
                        Diagnostics.Log($"[Gedemon] City {cityPosition.Name} at ({cityPosition.Column},{cityPosition.Row})");
                    }
                }
            }

        }

        public static bool TryGetCityNameAt(WorldPosition position, out string cityLocalizationKey)
        {
            cityLocalizationKey = null;
            int tileIndex = position.ToTileIndex();
            int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[tileIndex].TerritoryIndex;
            if(TerritoryCityMap.TryGetValue(territoryIndex, out List<CityPosition> cityList))
            {
                int bestDistance = int.MaxValue;
                foreach(CityPosition cityPosition in cityList)
                {
                    int column = cityPosition.Column;
                    int row = cityPosition.Row;
                    WorldPosition namePosition = new WorldPosition(column, row);
                    int distance = namePosition.GetDistance(tileIndex);
                    if(distance < bestDistance)
                    {
                        cityLocalizationKey = $"%{cityPosition.Name}";
                        bestDistance = distance;
                    }
                }
                if (cityLocalizationKey != null)
                    return true;

            }

            return false;
        }

        #endregion


        public static IDictionary<string, string> TranslationTable = new Dictionary<string, string>();
        static readonly List<string> SupportedTagsXML = new List<string> { "CityMap", "LocalizedText"};
        static readonly List<string> InsertTagsXML = new List<string> { "REPLACE", "INSERT" };
        public static void LoadXML(string input, string provider, bool inputIsText = false)
        {
            XmlReader xmlReader;

            if(inputIsText)
            {
                xmlReader = XmlReader.Create(new System.IO.StringReader(input));
            }
            else
            {
                xmlReader = XmlReader.Create(input);
            }

            string currentTag = null;
            while (xmlReader.Read())
            {
                if ((xmlReader.NodeType == XmlNodeType.Element))
                {
                    if (SupportedTagsXML.Contains(xmlReader.Name))
                    {
                        Diagnostics.LogWarning($"[Gedemon] [LoadXML] [Element] Current Tag = {xmlReader.Name}");
                        currentTag = xmlReader.Name;
                    }

                    if(currentTag != null)
                    {
                        if (xmlReader.HasAttributes)
                        {
                            if(InsertTagsXML.Contains(xmlReader.Name.ToUpper()))
                            {
                                switch(currentTag)
                                {
                                    case "CityMap":
                                        if(xmlReader.GetAttribute("MapName") == "GiantEarth")
                                        {
                                            //Diagnostics.Log($"[Gedemon] [LoadXML] [Element] <{xmlReader.Name}> : Column = {xmlReader.GetAttribute("X")}, Row = {xmlReader.GetAttribute("Y")}, Name = {xmlReader.GetAttribute("CityLocaleName")}");
                                            CityPosition cityPosition = new CityPosition { Name = xmlReader.GetAttribute("CityLocaleName"), Column = int.Parse(xmlReader.GetAttribute("X")), Row = int.Parse(xmlReader.GetAttribute("Y")) };
                                            if (!WorldCityMap.Contains(cityPosition))
                                                WorldCityMap.Add(cityPosition);
                                        }
                                        break;
                                    case "LocalizedText":
                                        //Diagnostics.Log($"[Gedemon] [LoadXML] [Element] <{xmlReader.Name}> : Column = {xmlReader.GetAttribute("X")}, Row = {xmlReader.GetAttribute("Y")}, Name = {xmlReader.GetAttribute("CityLocaleName")}");
                                        string Tag = xmlReader.GetAttribute("Tag");
                                        string Text = xmlReader.GetAttribute("Text");
                                        if (!TranslationTable.ContainsKey(Tag))
                                            TranslationTable.Add(Tag, Text);
                                        break;

                                }
                            }
                        }
                    }
                }
                // to do : add data as MapTCL class

            }
        }
        public static void UpdateTranslationDB()
        {
            var localizedStrings = Databases.GetDatabase<LocalizedStringElement>();
            foreach (KeyValuePair<string, string> kvp in TranslationTable)
            {
                localizedStrings.Touch(new LocalizedStringElement()
                {
                    LineId = $"%{kvp.Key}",
                    LocalizedStringElementFlag = LocalizedStringElementFlag.None,
                    CompactedNodes = new LocalizedNode[] {
                        new LocalizedNode{ Id= LocalizedNodeType.Terminal, TextValue=kvp.Value}
                    },
                    TagCodes = new[] { 0 }
                });
            }
        }
        public static void OnSandboxStarted()
        {
            UpdateTranslationDB();
        }
        public static void OnExitSandbox()
        {
            WorldCityMap.Clear();
            TerritoryCityMap.Clear();
            TranslationTable.Clear();
        }
    }
}
