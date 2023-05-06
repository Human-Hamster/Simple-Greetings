using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGreetings.GameData
{
    public class TerritoryData
    {
        public string Name { get; set; } = null!;
        public uint TerritoryType { get; set; }
        public string RawString { get; set; } = null!;
        public string InstanceType { get; set; } = null!;
        public uint MapId { get; set; }
    }

    public enum ContentType
    {
        Dungeon,
        Roulette,
        Raid,
        Alliance
    }

    public enum MacroType
    {
        Individual,
        Shared
    }
}

