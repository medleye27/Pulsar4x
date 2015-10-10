﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public static class RefiningProcessor
    {
        private const int _timeBetweenRuns = 68400; //one terran day.

        internal static void Initialize(StaticDataStore staticData)
        {
            
        }

        internal static void Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            foreach (var system in systems)
            {
                system.EconLastTickRun += deltaSeconds;
                if (system.EconLastTickRun >= _timeBetweenRuns)
                {
                    foreach (Entity colonyEntity in system.SystemManager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
                    {
                        RefineMaterials(colonyEntity, game);
                    }
                    system.EconLastTickRun -= _timeBetweenRuns;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static void RefineMaterials(Entity colony, Game game)
        {

            JDictionary<Guid, int> mineralStockpile = colony.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            JDictionary<Guid, int> materialsStockpile = colony.GetDataBlob<ColonyInfoDB>().RefinedStockpile;

            ColonyRefiningDB refiningDB = colony.GetDataBlob<ColonyRefiningDB>();


            for (int jobIndex = 0; jobIndex < refiningDB.JobBatchList.Count; jobIndex++)
            {
                var job = refiningDB.JobBatchList[jobIndex];


                RefinedMaterialSD material = game.StaticData.RefinedMaterials[job.jobGuid];
                Dictionary<Guid, int> mineralCosts = material.RawMineralCosts;
                Dictionary<Guid, int> materialCosts = material.RefinedMateraialsCosts;

                while (job.numberCompleted < job.numberOrdered && job.pointsLeft > material.RefinaryPointCost)
                {
                    if (HasReqiredItems(mineralStockpile, mineralCosts) && HasReqiredItems(materialsStockpile, materialCosts))
                    {
                        UseFromStockpile(mineralStockpile, mineralCosts);
                        UseFromStockpile(materialsStockpile, materialCosts);
                        refiningDB.RefinaryPoints -= material.RefinaryPointCost;
                        job.numberCompleted ++;
                        materialsStockpile.SafeValueAdd(material.ID, material.OutputAmount);
                    }
                }
                if (job.numberCompleted == job.numberOrdered)
                {
                    refiningDB.JobBatchList.RemoveAt(jobIndex);
                    if (job.auto)
                        refiningDB.JobBatchList.Add(job);
                }
            }
        }

        public static bool HasReqiredItems(JDictionary<Guid, int> stockpile, Dictionary<Guid, int> costs )
        {
            return costs.All(kvp => stockpile.ContainsKey(kvp.Key) || !(stockpile[kvp.Key] > kvp.Value));
        }

        public static void UseFromStockpile(JDictionary<Guid, int> stockpile, Dictionary<Guid, int> costs)
        {
            foreach (var kvp in costs)
            {
                stockpile[kvp.Key] -= kvp.Value;
            }
        }


        /// <summary>
        /// called by ReCalcProcessor
        /// </summary>
        /// <param name="colonyEntity"></param>
        public static void ReCalcRefiningRate(Entity colonyEntity)
        {
            List<Entity> installations = colonyEntity.GetDataBlob<ColonyInfoDB>().Installations;
            List<Entity> refinarys = new List<Entity>();
            foreach (var inst in installations)
            {
                if (inst.HasDataBlob<RefineResourcesDB>())
                    refinarys.Add(inst);
            }
            int rate = 0;
            foreach (var refinary in refinarys)
            {
                rate += refinary.GetDataBlob<RefineResourcesDB>().RefinaryPoints;
            }
            colonyEntity.GetDataBlob<ColonyRefiningDB>().RefinaryPoints = rate;
        }

        public static void AddJob(StaticDataStore staticData, Entity colonyEntity, RefineingJob job)
        {
            ColonyRefiningDB refiningDB = colonyEntity.GetDataBlob<ColonyRefiningDB>();
            lock (refiningDB.JobBatchList) //prevent threaded race conditions
            {
                if (staticData.RefinedMaterials.ContainsKey(job.jobGuid))
                    refiningDB.JobBatchList.Add(job);
            }
        }

        public static void MoveJob(Entity colonyEntity, RefineingJob job, int delta)
        {
            ColonyRefiningDB refiningDB = colonyEntity.GetDataBlob<ColonyRefiningDB>();
            lock (refiningDB.JobBatchList) //prevent threaded race conditions
            {
                if (refiningDB.JobBatchList.Contains(job))
                {
                    int currentIndex = refiningDB.JobBatchList.IndexOf(job);
                    int newIndex = currentIndex + delta;
                    if (newIndex <= 0)
                    {
                        refiningDB.JobBatchList.RemoveAt(currentIndex);
                        refiningDB.JobBatchList.Insert(0, job);
                    }
                    else if (newIndex >= refiningDB.JobBatchList.Count - 1)
                    {
                        refiningDB.JobBatchList.RemoveAt(currentIndex);
                        refiningDB.JobBatchList.Add(job);
                    }
                    else
                    {
                        refiningDB.JobBatchList.RemoveAt(currentIndex);
                        refiningDB.JobBatchList.Insert(newIndex,job);
                    }
                }
            }
        }

    }
}