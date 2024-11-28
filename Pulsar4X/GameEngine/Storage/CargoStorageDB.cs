using Newtonsoft.Json;
using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Storage
{
/// <summary>
/// TODO: this needs to be made threadsafe for reading form the ui!
/// </summary>
    public class CargoStorageDB : BaseDataBlob, IAbilityDescription
    {
        public Dictionary<string, TypeStore> TypeStores = new Dictionary<string, TypeStore>();

        internal List<CargoTransferObject> EscroItems { get; } = new();
        
        /// <summary>
        /// This includes Escro Items.
        /// </summary>
        public double TotalStoredMass { get; internal set; } = 0;

        /// <summary>
        /// kg per second.
        /// </summary>
        public int TransferRate { get; internal set; } = 5;

        public double TransferRangeDv_mps { get; internal set; } = 100;

        [JsonConstructor]
        internal CargoStorageDB()
        {
        }

        public CargoStorageDB(string type, double maxVolume)
        {
            TypeStores.Add(type, new TypeStore(maxVolume));
        }


        public CargoStorageDB(CargoStorageDB db)
        {
            TypeStores = new Dictionary<string, TypeStore>();
            foreach (var kvp in db.TypeStores)
            {
                TypeStores.Add(kvp.Key, kvp.Value.Clone());
            }
            TotalStoredMass = db.TotalStoredMass;
            TransferRangeDv_mps = db.TransferRangeDv_mps;
            TransferRate = db.TransferRate;
        }

        public override object Clone()
        {
            return new CargoStorageDB(this);
        }

        public string AbilityName()
        {
            return "Cargo Volume";
        }

        public string AbilityDescription()
        {
            string desc = "Total Volume storage\n";
            foreach (var kvp in TypeStores)
            {
                //string name = StaticRefLib.StaticData.CargoTypes[kvp.Key].Name;
                //desc += name + "\t" + kvp.Value.MaxVolume + "\n";
                desc += kvp.Value.MaxVolume + "\n";
            }

            return desc;
        }
    }

    public class TypeStore
    {
        public double MaxVolume;
        internal double FreeVolume;
        /// <summary>
        /// Key is ICargoable.ID
        /// </summary>
        public SafeDictionary<int, long> CurrentStoreInUnits = new ();
        /// <summary>
        /// Key is ICargoable.ID
        /// </summary>
        internal Dictionary<int, ICargoable> Cargoables =  new ();
        public TypeStore(double maxVolume)
        {
            MaxVolume = maxVolume;
            FreeVolume = maxVolume;
        }

        public Dictionary<int, ICargoable> GetCargoables()
        {
            return new (Cargoables);
        }

        public bool HasCargoInStore(int cargoID)
        {
            return CurrentStoreInUnits.ContainsKey(cargoID);
        }
        
        public TypeStore Clone()
        {
            TypeStore clone = new TypeStore(MaxVolume);
            clone.FreeVolume = FreeVolume;
            clone.CurrentStoreInUnits = new SafeDictionary<int, long>(CurrentStoreInUnits);
            clone.Cargoables = new Dictionary<int, ICargoable>(Cargoables);
            return clone;
        }

    }
}
