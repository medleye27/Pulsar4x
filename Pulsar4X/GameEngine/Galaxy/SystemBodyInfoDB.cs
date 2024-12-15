using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.DataStructures;
using Pulsar4X.Names;
using Pulsar4X.Sensors;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Galaxy
{
    /// <summary>
    /// SystemBodyInfoDB defines an entity as having properties like planets/asteroids/coments.
    /// </summary>
    /// <remarks>
    /// Specifically, Minerals, body info, atmosphere info, and gravity.
    /// </remarks>
    public class SystemBodyInfoDB : BaseDataBlob, ISensorCloneMethod
    {
        public new static List<Type> GetDependencies() => new List<Type>() { typeof(NameDB) };
        /// <summary>
        /// Type of body this is. <see cref="BodyType"/>
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public BodyType BodyType { get; internal set; }

        /// <summary>
        /// Gets or sets the albedo(Bond).
        /// </summary>
        /// <value>The albedo(Bond)</value>
        [JsonProperty]
        public PercentValue Albedo { get; internal set; }

        /// <summary>
        /// Plate techtonics. Ammount of activity depends on age vs mass.
        /// Influences magnitic field. maybe this should be in the processor?
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public TectonicActivity Tectonics { get; internal set; }

        /// <summary>
        /// The Axial Tilt of this body.
        /// Measured in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float AxialTilt { get; internal set; }

        /// <summary>
        /// Magnetic field of the body. It is important as it affects how much atmosphere a body will have.
        /// In Microtesla (uT)
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float MagneticField { get; internal set; }

        /// <summary>
        /// Temperature of the planet BEFORE greenhouse effects are taken into consideration.
        /// This is mostly a factor of how much light reaches the planet nad is calculated at generation time.
        /// In Degrees C.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float BaseTemperature { get; internal set; }

        /// <summary>
        /// Amount of radiation on this body. Affects ColonyCost.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float RadiationLevel { get; internal set; }

        /// <summary>
        /// Amount of atmosphic dust on this body. Affects ColonyCost.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public float AtmosphericDust { get; internal set; }

        /// <summary>
        /// Indicates if the system body supports populations and can be settled by Players/NPRs.
        /// </summary>
        /// <remarks>
        /// TODO: Gameplay Review
        /// See if we want to decide SupportsPopulations some other way.
        /// Some species may be capable of habiting different types and different bodies.
        /// Maybe this should be at the species level.
        /// </remarks>
        [PublicAPI]
        [JsonProperty]
        public bool SupportsPopulations { get; internal set; }

        /// <summary>
        /// Length of day for this body. Mostly fluff.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public TimeSpan LengthOfDay { get; internal set; }

        /// <summary>
        /// Gravity on this body measured in m/s/s. Affects ColonyCost.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Gravity { get; internal set; }


        /// <summary>
        /// Stores the amount of the various minerials. the guid can be used to lookup the
        /// minerial definition (MineralSD) from the StaticDataStore.
        /// </summary>
        // [PublicAPI]
        // [JsonProperty]
        // public Dictionary<Guid, MineralDepositInfo> Minerals { get; internal set; }

        public SystemBodyInfoDB()
        {
        }

        public SystemBodyInfoDB(SystemBodyInfoDB systemBodyDB)
        {
            BodyType = systemBodyDB.BodyType;
            Tectonics = systemBodyDB.Tectonics;
            AxialTilt = systemBodyDB.AxialTilt;
            MagneticField = systemBodyDB.MagneticField;
            BaseTemperature = systemBodyDB.BaseTemperature;
            RadiationLevel = systemBodyDB.RadiationLevel;
            AtmosphericDust = systemBodyDB.AtmosphericDust;
            SupportsPopulations = systemBodyDB.SupportsPopulations;
            LengthOfDay = systemBodyDB.LengthOfDay;
            Gravity = systemBodyDB.Gravity;
        }

        public override object Clone()
        {
            return new SystemBodyInfoDB(this);
        }

        public BaseDataBlob SensorClone(SensorInfoDB sensorInfo)
        {
            return new SystemBodyInfoDB(this, sensorInfo);
        }

        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            UpdateDatablob(sensorInfo.DetectedEntity.GetDataBlob<SystemBodyInfoDB>(), sensorInfo);
        }

        void UpdateDatablob(SystemBodyInfoDB originalDB, SensorInfoDB sensorInfo)
        {
            Random rng = new Random(); //TODO: rand should be deterministic.
            float accuracy = sensorInfo.HighestDetectionQuality.SignalQuality;

            if (sensorInfo.HighestDetectionQuality.SignalQuality > 0.20)
                BodyType = originalDB.BodyType;
            else
                BodyType = BodyType.Unknown;
            if (sensorInfo.HighestDetectionQuality.SignalQuality > 0.80)
                Tectonics = originalDB.Tectonics;
            else
                Tectonics = TectonicActivity.Unknown;
            //TODO: #SensorClone, #TMI more random to the rest of it.
            var tilt = SensorTools.RndSigmoid(originalDB.AxialTilt, accuracy, rng);
            AxialTilt = (float)tilt;
            MagneticField = originalDB.MagneticField;
            BaseTemperature = originalDB.BaseTemperature;
            RadiationLevel = originalDB.RadiationLevel;
            AtmosphericDust = originalDB.AtmosphericDust;
            SupportsPopulations = originalDB.SupportsPopulations;
            LengthOfDay = originalDB.LengthOfDay;
            Gravity = originalDB.Gravity;
        }

        SystemBodyInfoDB(SystemBodyInfoDB originalDB, SensorInfoDB sensorInfo)
        {
            UpdateDatablob(originalDB, sensorInfo);
        }
    }
}