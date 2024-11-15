﻿using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class MiningTests
    {
        private string _mineDesignGUID = "mine";
        private Game _game;
        private AuthenticationToken _smAuthToken;

        [Test]
        [Description("Creates and tests the Sol star system with humans with resources")]
        public void TestHumansOnEarthInSol()
        {
            var modLoader = new ModLoader();
            var modDataStore = new ModDataStore();

            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            var startDate = new DateTime(2050, 1, 1);
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0 }, modDataStore); // reinit with empty game, so we can do a clean test.
            _game.Settings.EnableMultiThreading = true;
            _game.Settings.EnforceSingleThread = false;

            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);

            var humanFaction = FactionFactory.CreateFaction(_game, "Human Empire");
            var humanSpecies = SpeciesFactory.CreateSpeciesHuman(humanFaction, _game.GlobalManager);
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSol(_game);


            // lets test that the stars generated okay:
            List<Entity> stars = system.GetAllEntitiesWithDataBlob<StarInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(stars);
            Assert.AreEqual(stars.Count, 1);

            StarInfoDB sol = stars[0].GetDataBlob<StarInfoDB>();

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(systemBodies);

            List<SystemBodyInfoDB> bodies = system.GetAllDataBlobsOfType<SystemBodyInfoDB>();

            // Earth
            var earth = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Earth"));
            Assert.IsNotNull(earth);

            //var earthResources = earth.Minerals.ToDictionary(k => mineralsList.FirstOrDefault(x => x.ID == k.Key).Name, v => v.Value);
            //Assert.AreEqual(19, earthResources.Keys.Count);

            // Uncomment following lines and run as debug to regenerate expected value checks if resource list or generation changes.
            // GENERATOR CODE
            //string s = "";
            //earthResources.ToList().ForEach(x => s += "CheckLevels(earthResources, \"" + x.Key + "\", " + Math.Round(x.Value.Accessibility, 6) + ", " + x.Value.Amount + ");\r\n");
            // END OF GENERATOR

            // CheckLevels(earthResources, "Hydrocarbons", 1, 204321);
            // CheckLevels(earthResources, "Iron", 1, 93278);
            // CheckLevels(earthResources, "Aluminium", 0.854084, 144386);
            // CheckLevels(earthResources, "Copper", 0.601266, 114246);
            // CheckLevels(earthResources, "Titanium", 0.528637, 74803);
            // CheckLevels(earthResources, "Lithium", 0.82011, 148977);
            // CheckLevels(earthResources, "Chromium", 1, 102391);
            // CheckLevels(earthResources, "Fissionables", 0.782729, 111535);
            // CheckLevels(earthResources, "Sorium", 1, 120180);
            // CheckLevels(earthResources, "Duranium", 1, 59339);
            // CheckLevels(earthResources, "Neutronium", 0.628726, 80558);
            // CheckLevels(earthResources, "Corbomite", 1, 66947);
            // CheckLevels(earthResources, "Tritanium", 0.579378, 53072);
            // CheckLevels(earthResources, "Boronide", 1, 112053);
            // CheckLevels(earthResources, "Uridium", 0.889412, 100171);
            // CheckLevels(earthResources, "Corundium", 0.989507, 50953);
            // CheckLevels(earthResources, "Mercassium", 1, 130630);
            // CheckLevels(earthResources, "Vendarite", 1, 56250);
            // CheckLevels(earthResources, "Gallicite", 1, 102526);

            var earthColony = SetupColony(humanFaction, humanSpecies, earth.OwningEntity);

            //Assert.IsTrue(earth.Colonies.Any(), "Earth should have a colony on it.");
            var earthComponents = earthColony.OwningEntity.GetDataBlob<ComponentInstancesDB>();
            List<ComponentInstance> miningInstallations;
            earthComponents.TryGetComponentsByAttribute<MineResourcesAtbDB>(out miningInstallations);

            Assert.AreEqual(10, miningInstallations.Count);
            Assert.AreEqual(500000000, earthColony.Population.Values.First());
            Assert.AreEqual(1800, earthColony.OwningEntity.GetDataBlob<MiningDB>().BaseMiningRate.Sum(x => x.Value));

            _game.TimePulse.Ticklength = TimeSpan.FromHours(24);
            var targetDate = _game.TimePulse.GameGlobalDateTime.AddHours(24);
            _game.TimePulse.TimeStep();     // Jump forward 1 day

            while (_game.TimePulse.GameGlobalDateTime < targetDate)
            {
                // wait timeStep to finish
            }

            bodies = _game.Systems.First().GetAllDataBlobsOfType<SystemBodyInfoDB>();
            earth = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Earth"));
            //var earthResourcesAfter = earth.Minerals.ToDictionary(k => mineralsList.FirstOrDefault(x => x.ID == k.Key).Name, v => v.Value);
            // Uncomment following lines and run as debug to regenerate expected value checks if resource list or generation changes.
            // GENERATOR CODE
            //string s = "";
            //earthResources.ToList().ForEach(x => s += "CheckLevels(earthResourcesAfter, \"" + x.Key + "\", " + Math.Round(x.Value.Accessibility, 6) + ", " + x.Value.Amount + ");\r\n");
            // END OF GENERATOR

            // Check resources have depleted by expected amounts
            // CheckLevels(earthResourcesAfter, "Hydrocarbons", 1, 204221);
            // CheckLevels(earthResourcesAfter, "Iron", 1, 93178);
            // CheckLevels(earthResourcesAfter, "Aluminium", 0.854084, 144301);
            // CheckLevels(earthResourcesAfter, "Copper", 0.601266, 114186);
            // CheckLevels(earthResourcesAfter, "Titanium", 0.528637, 74803);
            // CheckLevels(earthResourcesAfter, "Lithium", 0.82011, 148895);
            // CheckLevels(earthResourcesAfter, "Chromium", 1, 102291);
            // CheckLevels(earthResourcesAfter, "Fissionables", 0.782729, 111457);
            // CheckLevels(earthResourcesAfter, "Sorium", 1, 120080);
            // CheckLevels(earthResourcesAfter, "Duranium", 1, 59239);
            // CheckLevels(earthResourcesAfter, "Neutronium", 0.628726, 80495);
            // CheckLevels(earthResourcesAfter, "Corbomite", 1, 66847);
            // CheckLevels(earthResourcesAfter, "Tritanium", 0.579378, 53014);
            // CheckLevels(earthResourcesAfter, "Boronide", 1, 111953);
            // CheckLevels(earthResourcesAfter, "Uridium", 0.889412, 100082);
            // CheckLevels(earthResourcesAfter, "Corundium", 0.989507, 50854);
            // CheckLevels(earthResourcesAfter, "Mercassium", 1, 130530);
            // CheckLevels(earthResourcesAfter, "Vendarite", 1, 56150);
            // CheckLevels(earthResourcesAfter, "Gallicite", 1, 102426);

        }

        private ColonyInfoDB SetupColony(Entity faction, Entity species, Entity planet)
        {
            var colonyEntity = ColonyFactory.CreateColony(faction, species, planet, 500000000);
            var dataStore = faction.GetDataBlob<FactionInfoDB>().Data;
            // Mines
            ComponentTemplateBlueprint mineSD = dataStore.ComponentTemplates[_mineDesignGUID];
            ComponentDesigner mineDesigner = new ComponentDesigner(mineSD, dataStore, faction.GetDataBlob<FactionTechDB>());
            ComponentDesign mineDesign = mineDesigner.CreateDesign(faction);

            colonyEntity.AddComponent(mineDesign, 10);

            ComponentDesign cargoInstallation = DefaultStartFactory.DefaultCargoInstallation(faction, dataStore);
            colonyEntity.AddComponent(cargoInstallation, 10);

            ReCalcProcessor.ReCalcAbilities(colonyEntity);

            return colonyEntity.GetDataBlob<ColonyInfoDB>();
        }

        private void CheckLevels(Dictionary<string, MineralDeposit> resourceData, string resource, double accessibility, long quantity)
        {
            Assert.IsTrue(resourceData.ContainsKey(resource));
            var depositInfo = resourceData[resource];
            Assert.AreEqual(Math.Round(accessibility, 6), Math.Round(depositInfo.Accessibility, 6), resource + " is not at expected availability level.");
            Assert.AreEqual(quantity, depositInfo.Amount, resource + " is not at expected amount.");
        }
    }
}