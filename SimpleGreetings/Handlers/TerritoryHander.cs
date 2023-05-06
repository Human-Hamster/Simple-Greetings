using System;
using System.Collections.Generic;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using SimpleGreetings.GameData;
using System.Text.Json;
using System.IO;

namespace SimpleGreetings.Handlers
{
    public class TerritoryHandler
    {
        private readonly DataManager _dmgr;
        private readonly Dictionary<uint, TerritoryData> territoryDatabase;

        public TerritoryHandler(DataManager data) {
            this._dmgr = data;
            this.territoryDatabase = new Dictionary<uint, TerritoryData>();

            //string fileName = "C:/Users/sefto/Desktop/TerritoryTypesSheet.json";

            foreach(var territory in data.GetExcelSheet<TerritoryType>())
            {
                string instanceType;

                // Let's store only territories that have instance types. E.g. housing, dungeon etc.
                try {
                    instanceType = territory.Bg.RawString.Split('/')[2].ToLower();
                } catch {
                    continue;
                }

                //File.AppendAllText(fileName, territory.Bg.RawString);

                if (!this.territoryDatabase.ContainsKey(territory.RowId)) {
                    this.territoryDatabase.Add(territory.RowId, new TerritoryData
                    {
                        Name = territory.Map.Value.PlaceName.Value.Name,
                        RawString = territory.Bg.RawString,
                        MapId = territory.Map.Value.RowId,
                        InstanceType = instanceType,
                        TerritoryType = territory.RowId
                    });
                }
            }
        }

        public TerritoryData getTerritoryData(ushort territoryId)
        {
            if (!this.territoryDatabase.ContainsKey((uint)territoryId))
            {
                return new TerritoryData { };
            }

            return this.territoryDatabase[(uint)territoryId];

        }
    }
}
