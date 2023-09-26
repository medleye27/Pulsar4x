using System.Collections.Generic;

namespace Pulsar4X.Modding
{
    public class ThemeBlueprint : SerializableGameData
    {
        public string Name { get; set; }
        public List<string> FleetNames { get; set; }
        public List<string> ShipNames { get; set; }
        public List<string> FirstNames { get; set; }
        public List<string> LastNames { get; set; }
        public Dictionary<int, string> NavyRanks { get; set; }
        public Dictionary<int, string> NavyRanksAbbreviations { get; set; }
    }
}