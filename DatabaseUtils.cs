using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Localization;
using Amplitude.Mercury;
using Amplitude.Mercury.Simulation;

namespace Gedemon.TrueCultureLocation
{
    class DatabaseUtils
    {
        static List<int> GetListIntFromString(string text)
        {
            string[] elements = text.Split(',');
            List<int> listValues = new List<int>();
            for(int i = 0; i < elements.Length; i++)
            {
                if(int.TryParse(elements[i], out int value))
                {
                    listValues.Add(value);
                }
            }
            return listValues;
        }

        static bool TryGetAttribute(XmlReader reader, string attribute, out string result)
        {
            result = reader.GetAttribute(attribute);
            return result != null;
        }

        static bool TryGetFactionTerritoriesRow(XmlReader reader, out string civilization, out List<int> territories)
        {
            civilization = reader.GetAttribute("Civilization");
            territories = null;
            if(TryGetAttribute(reader, "Territories", out string territoriesAttribute))
            {
                territories = GetListIntFromString(territoriesAttribute);
            }
            return (civilization != null && territories != null);
        }
        static bool TryGetIndexNameRow(XmlReader reader, out string name, out int index)
        {
            name = reader.GetAttribute("Name");
            index = -1;
            if (TryGetAttribute(reader, "Index", out string indexAttribute))
            {
                index = int.Parse(indexAttribute);
            }
            return (name != null && index != -1);
        }
        static bool TryGetIndexPositionRow(XmlReader reader, out int index, out Hexagon.OffsetCoords position)
        {
            position = new Hexagon.OffsetCoords(-1,-1);
            index = -1;
            if (TryGetAttribute(reader, "Index", out string indexAttribute))
            {
                index = int.Parse(indexAttribute);
            }
            if (TryGetAttribute(reader, "X", out string xAttribute) && TryGetAttribute(reader, "Y", out string yAttribute))
            {
                if(int.TryParse(xAttribute, out int x) && int.TryParse(yAttribute, out int y))
                {
                    position = new Hexagon.OffsetCoords(x, y);
                }
            }
            return (position.Column != -1 && position.Row != -1 && index != -1);
        }
        static bool TryGetCityMapRow(XmlReader reader, out CityPosition cityPosition)
        {
            cityPosition = new CityPosition();
            if (TryGetAttribute(reader, "Tag", out string localizationKey))
            {
                if (TryGetAttribute(reader, "X", out string xAttribute) && TryGetAttribute(reader, "Y", out string yAttribute))
                {
                    if (int.TryParse(xAttribute, out int x) && int.TryParse(yAttribute, out int y))
                    {
                        cityPosition.Name = localizationKey;
                        cityPosition.Column = x;
                        cityPosition.Row = y;
                        return true;
                    }
                }
            }
            return false;
        }

        public static IDictionary<string, string> TranslationTable = new Dictionary<string, string>();
        static readonly List<string> SupportedTablesXML = new List<string> { "CivilizationCityAliases", "LocalizedText", "CityMap", "MajorEmpireTerritories", "MajorEmpireCoreTerritories", "MinorFactionTerritories", "ExtraPositions", "ExtraPositionsNewWorld", "ContinentNames", "TerritoryNames", "NoCapital", "NomadCultures" };
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

            IList<MapTCL> moddedTCL = new List<MapTCL>();
            MapTCL currentMapTCL = null;
            string currentMapName = null;

            string currentTable = null;
            while (xmlReader.Read())
            {
                if ((xmlReader.NodeType == XmlNodeType.Element))
                {
                    switch(xmlReader.Name)
                    {
                        case "Map":
                            TryGetAttribute(xmlReader, "Name", out currentMapName);
                            Diagnostics.LogWarning($"[Gedemon] [LoadXML] [Element] Switch current Map (Name = {currentMapName})");
                            if (TryGetAttribute(xmlReader, "MapTerritoryHash", out string hashList))
                            {
                                List<int> mapTerritoryHash = GetListIntFromString(hashList);
                                if (currentMapTCL != null)
                                {
                                    moddedTCL.Add(currentMapTCL);
                                }
                                currentMapTCL = new MapTCL { MapTerritoryHash = mapTerritoryHash };
                                if (TryGetAttribute(xmlReader, "LoadOrder", out string loadOrderAttribute))
                                {
                                    int loadOrder = int.Parse(loadOrderAttribute);
                                    currentMapTCL.LoadOrder = loadOrder;
                                }
                            }
                            else
                            {
                                Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't initialize MapTCL, missing attribute (currentMapName = {currentMapName}) (MapTerritoryHash = {hashList})");
                            }
                            break;
                    }

                    if (SupportedTablesXML.Contains(xmlReader.Name))
                    {
                        Diagnostics.LogWarning($"[Gedemon] [LoadXML] [Element] Switch current Table to {xmlReader.Name}");
                        currentTable = xmlReader.Name;
                    }

                    if(currentTable != null)
                    {
                        if (xmlReader.HasAttributes)
                        {
                            switch (currentTable)
                            {
                                #region MapTCL
                                case "MajorEmpireTerritories":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.MajorEmpireTerritories == null)
                                            currentMapTCL.MajorEmpireTerritories = new Dictionary<string, List<int>>();

                                        if (TryGetFactionTerritoriesRow(xmlReader, out string civilization, out List<int> territories) && !currentMapTCL.MajorEmpireTerritories.ContainsKey(civilization))
                                        {
                                            currentMapTCL.MajorEmpireTerritories.Add(civilization, territories);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Faction/Territories Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "MajorEmpireCoreTerritories":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.MajorEmpireCoreTerritories == null)
                                            currentMapTCL.MajorEmpireCoreTerritories = new Dictionary<string, List<int>>();

                                        if (TryGetFactionTerritoriesRow(xmlReader, out string civilization, out List<int> territories) && !currentMapTCL.MajorEmpireCoreTerritories.ContainsKey(civilization))
                                        {
                                            currentMapTCL.MajorEmpireCoreTerritories.Add(civilization, territories);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Faction/Territories Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "MinorFactionTerritories":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.MinorFactionTerritories == null)
                                            currentMapTCL.MinorFactionTerritories = new Dictionary<string, List<int>>();

                                        if (TryGetFactionTerritoriesRow(xmlReader, out string civilization, out List<int> territories) && !currentMapTCL.MinorFactionTerritories.ContainsKey(civilization))
                                        {
                                            currentMapTCL.MinorFactionTerritories.Add(civilization, territories);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Faction/Territories Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "ContinentNames":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.ContinentNames == null)
                                            currentMapTCL.ContinentNames = new Dictionary<int, string>();

                                        if (TryGetIndexNameRow(xmlReader, out string name, out int index) && !currentMapTCL.ContinentNames.ContainsKey(index))
                                        {
                                            currentMapTCL.ContinentNames.Add(index, name);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Index/Name Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "TerritoryNames":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.TerritoryNames == null)
                                            currentMapTCL.TerritoryNames = new Dictionary<int, string>();

                                        if (TryGetIndexNameRow(xmlReader, out string name, out int index) && !currentMapTCL.TerritoryNames.ContainsKey(index))
                                        {
                                            currentMapTCL.TerritoryNames.Add(index, name);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Index/Name Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "ExtraPositions":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.ExtraPositions == null)
                                            currentMapTCL.ExtraPositions = new Dictionary<int, Hexagon.OffsetCoords>();

                                        if (TryGetIndexPositionRow(xmlReader, out int index, out Hexagon.OffsetCoords position) && !currentMapTCL.ExtraPositions.ContainsKey(index))
                                        {
                                            currentMapTCL.ExtraPositions.Add(index, position);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Index/Name Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "ExtraPositionsNewWorld":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.ExtraPositionsNewWorld == null)
                                            currentMapTCL.ExtraPositionsNewWorld = new Dictionary<int, Hexagon.OffsetCoords>();

                                        if (TryGetIndexPositionRow(xmlReader, out int index, out Hexagon.OffsetCoords position) && !currentMapTCL.ExtraPositionsNewWorld.ContainsKey(index))
                                        {
                                            currentMapTCL.ExtraPositionsNewWorld.Add(index, position);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add Index/Name Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "NoCapital":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.NoCapital == null)
                                            currentMapTCL.NoCapital = new List<string>();

                                        string civilization = xmlReader.GetAttribute("Civilization");
                                        if (civilization != null && !currentMapTCL.NoCapital.Contains(civilization))
                                        {
                                            currentMapTCL.NoCapital.Add(civilization);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't read Civilization Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "NomadCultures":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.NomadCultures == null)
                                            currentMapTCL.NomadCultures = new List<string>();

                                        string civilization = xmlReader.GetAttribute("Civilization");
                                        if (civilization != null && !currentMapTCL.NomadCultures.Contains(civilization))
                                        {
                                            currentMapTCL.NomadCultures.Add(civilization);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't read Civilization Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                case "CityMap":
                                    if (currentMapTCL != null)
                                    {
                                        if (currentMapTCL.CityMap == null)
                                            currentMapTCL.CityMap = new List<CityPosition>();

                                        if (TryGetCityMapRow(xmlReader, out CityPosition cityPosition))
                                        {
                                            currentMapTCL.CityMap.Add(cityPosition);
                                        }
                                        else
                                        {
                                            IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                            Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add CityMap Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                        }
                                    }
                                    else
                                    {
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't register row (current table = {currentTable}): MapTCL is not initialized");
                                    }
                                    break;
                                #endregion
                                case "CivilizationCityAliases":
                                    if (TryGetAttribute(xmlReader, "Civilization", out string Civilization) && TryGetAttribute(xmlReader, "Aliases", out string Aliases) && !TranslationTable.ContainsKey(Civilization))
                                    {                                        
                                        CityMap.CivilizationAliases.Add(Civilization, Aliases.Split(',').ToList());
                                    }
                                    else
                                    {
                                        IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add CivilizationAliases Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                    }
                                    break;
                                case "LocalizedText":
                                    if (TryGetAttribute(xmlReader, "Tag", out string Tag) && TryGetAttribute(xmlReader, "Text", out string Text) && !TranslationTable.ContainsKey(Tag))
                                        TranslationTable.Add(Tag, Text);
                                    else
                                    {
                                        IXmlLineInfo xmlInfo = xmlReader as IXmlLineInfo;
                                        Diagnostics.LogError($"[Gedemon] [LoadXML] [Element] Can't add LocalizedText Row (current table = {currentTable}) at line = {xmlInfo.LineNumber}, position = {xmlInfo.LinePosition}");
                                    }
                                    break;

                            }
                        }
                    }
                }
                // to do : add data as MapTCL class

            }

            if (currentMapTCL != null)
            {
                moddedTCL.Add(currentMapTCL);
            }

            if (moddedTCL.Count > 0)
            {
                ModLoading.AddModdedTCL(moddedTCL, provider);
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
            TranslationTable.Clear();
        }

    }
}
