﻿/*
using System.Numerics;
using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using Pulsar4X.Industry;
using Pulsar4X.Galaxy;

namespace Pulsar4X.SDL2UI
{
    class UniquePlanetaryWindow : PulsarGuiWindow
    {
        private readonly List<Mineral> _mineralDefinitions = null;
        private readonly int _maxMineralNameLength = 0;
        private const string _amountFormat = "#,###,###,###,###,###,##0";   // big enough to render 64 integers

        private enum PlanetarySubWindows{
            generalInfo,
            installations,
            mineralDeposits
        }

        private EntityState _lookedAtEntity;

        private PlanetarySubWindows _selectedSubWindow = PlanetarySubWindows.generalInfo;

        internal UniquePlanetaryWindow(EntityState entity)
        {
            if (_mineralDefinitions == null) {
                _mineralDefinitions = _uiState.Game.StaticData.CargoGoods.GetMineralsList();
                _maxMineralNameLength = _mineralDefinitions.Max(x => x.Name.Length);
            }
            //_flags = ImGuiWindowFlags.NoCollapse;

            _flags = ImGuiWindowFlags.AlwaysAutoResize;
            onEntityChange(entity);
        }

        internal void onEntityChange(EntityState entity)
        {
            _lookedAtEntity = entity;
        }

        internal static UniquePlanetaryWindow GetInstance(EntityState entity)
        {
            UniquePlanetaryWindow thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(UniquePlanetaryWindow)))
            {
                thisItem = new UniquePlanetaryWindow(entity);
            }
            else
            {
                thisItem = (UniquePlanetaryWindow)_uiState.LoadedWindows[typeof(UniquePlanetaryWindow)];
                thisItem.onEntityChange(entity);
            }


            return thisItem;
        }


        internal override void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button)
        {

        }

        internal override void Display()
        {
            ImGui.SetNextWindowSize(new Vector2(400,400),ImGuiCond.Once);
            if (IsActive == true && ImGui.Begin("Planetary Window: " + _lookedAtEntity.Name, ref IsActive, _flags))
            {
                RenderTabOptions();

                ImGui.BeginChild("data");
                switch(_selectedSubWindow){
                    case PlanetarySubWindows.generalInfo:
                        RenderGeneralInfo();
                        break;
                    case PlanetarySubWindows.installations:
                        RenderInstallations();
                        break;
                    case PlanetarySubWindows.mineralDeposits:
                        RenderMineralDeposits();
                        break;
                    default:
                        break;
                }
                ImGui.EndChild();
                ImGui.End();
            }
        }

        private void RenderTabOptions()
        {
            if (ImGui.SmallButton("General Info"))
            {
                _selectedSubWindow = PlanetarySubWindows.generalInfo;
            }

            if (_lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Installations"))
                {
                    _selectedSubWindow = PlanetarySubWindows.installations;
                }
            }

            if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>() && _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>().Minerals.Any())
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Mineral Deposits"))
                {
                    _selectedSubWindow = PlanetarySubWindows.mineralDeposits;
                }
            }
        }

        private void RenderGeneralInfo()
        {
            var headerRow = new List<KeyValuePair<string, TextAlign>>
            {
                new KeyValuePair<string, TextAlign>("", TextAlign.Left),
                new KeyValuePair<string, TextAlign>("", TextAlign.Right)
            };

            List<string[]> rowData = new List<string[]>();

            if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>())
            {
                SystemBodyInfoDB sysBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                rowData.Add(new string[] { "Body Type", sysBodyInfo.BodyType.ToDescription() });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<StarInfoDB>())
            {
                StarInfoDB starInfo = _lookedAtEntity.Entity.GetDataBlob<StarInfoDB>();
                rowData.Add(new string[] { "Spectral Type", starInfo.SpectralType.ToDescription() + starInfo.SpectralSubDivision });
                rowData.Add(new string[] { "Luminosity Type", starInfo.LuminosityClass.ToDescription() });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<MassVolumeDB>())
            {
                var tempMassVolume = _lookedAtEntity.Entity.GetDataBlob<MassVolumeDB>();
                rowData.Add(new string[] { "Radius", Stringify.Distance(tempMassVolume.RadiusInM) });
                rowData.Add(new string[] { "Mass", tempMassVolume.MassDry.ToString() + " kg" });
                rowData.Add(new string[] { "Volume", Stringify.Volume(tempMassVolume.Volume_m3) });
                rowData.Add(new string[] { "Density", tempMassVolume.Density_gcm.ToString("##0.000") + " kg/m^3" });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<ColonyInfoDB>())
            {
                rowData.Add(new string[] { "-----", "" });
                rowData.Add(new string[] { "Populations", "" });
                ColonyInfoDB tempColonyInfo = _lookedAtEntity.Entity.GetDataBlob<ColonyInfoDB>();
                foreach (var popPerSpecies in tempColonyInfo.Population)
                {
                    rowData.Add(new string[] {" " + popPerSpecies.Key.GetDefaultName(), Stringify.Quantity(popPerSpecies.Value, "0.0##", true) });
                }
            }


            if (_lookedAtEntity.Entity.HasDataBlob<StarInfoDB>())
            {
                StarInfoDB starInfo = _lookedAtEntity.Entity.GetDataBlob<StarInfoDB>();
                rowData.Add(new string[] { "Surface Temp", starInfo.Temperature.ToString("###,##0.00") + "°C" });
            }

            if (_lookedAtEntity.Entity.HasDataBlob<AtmosphereDB>())
            {
                AtmosphereDB atmosInfo = _lookedAtEntity.Entity.GetDataBlob<AtmosphereDB>();
                rowData.Add(new string[] { "-----", "" });
                rowData.Add(new string[] { "Hydroshpere", atmosInfo.Hydrosphere ? "YES" : "NO" });
                if (atmosInfo.Hydrosphere)
                {
                    rowData.Add(new string[] { "  Extent", atmosInfo.HydrosphereExtent.ToString() + " percent" });
                }

                if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>())
                {
                    SystemBodyInfoDB sysBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                    rowData.Add(new string[] { "Base Temp", sysBodyInfo.BaseTemperature.ToString("###,##0.00") + "°C" });
                }
                rowData.Add(new string[] { "Surface Temp", atmosInfo.SurfaceTemperature.ToString("###,##0.00") + "°C" });

                rowData.Add(new string[] { "-----", "" });
                rowData.Add(new string[] { "Atmosphere", "" });
                rowData.Add(new string[] { "Pressure", atmosInfo.Pressure + " atm" });

                rowData.Add(new string[] { "Composition", "" });
                foreach (var atmosGas in atmosInfo.Composition)
                {
                    rowData.Add(new string[] { "  " + atmosGas.Key.Name, Stringify.Quantity(atmosGas.Value, "0.0##") + " atm" });
                }
            }

            Helpers.RenderImgUITextTable(headerRow.ToArray(), rowData);
        }

        private void RenderInstallations()
        {
            if (_lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())
            {
                InstallationsDB tempInstallations = _lookedAtEntity.Entity.GetDataBlob<InstallationsDB>();
            }
        }

        private void RenderMineralDeposits()
        {
            var headerRow = new List<KeyValuePair<string, TextAlign>>
            {
                new KeyValuePair<string, TextAlign>("Mineral", TextAlign.Left),
                new KeyValuePair<string, TextAlign>("Available", TextAlign.Center),
                new KeyValuePair<string, TextAlign>("Accessibility", TextAlign.Right)
            };

            if (_lookedAtEntity.Entity.HasDataBlob<SystemBodyInfoDB>())
            {
                Dictionary<Guid, long> mineRates = new Dictionary<Guid, long>();

                SystemBodyInfoDB systemBodyInfo = _lookedAtEntity.Entity.GetDataBlob<SystemBodyInfoDB>();
                if (systemBodyInfo.Colonies.Any())
                {
                    // if colonies exists then
                    headerRow.Add(new KeyValuePair<string, TextAlign>("Mining Rate", TextAlign.Right));
                    foreach (Entity colonyEntity in systemBodyInfo.Colonies)
                    {
                        var colonyRates = MiningHelper.CalculateActualMiningRates(colonyEntity);
                        foreach (var rate in colonyRates)
                        {
                            if (!mineRates.ContainsKey(rate.Key))
                            {
                                mineRates.Add(rate.Key, 0);
                            }
                            mineRates[rate.Key] += rate.Value;
                        }
                    }
                }

                var deposits = systemBodyInfo.Minerals.Where(x => x.Value.Amount > 0);
                if (deposits.Any())
                {
                    var maxMineralQuantity = systemBodyInfo.Minerals.Values.Max(x => x.Amount).ToString(_amountFormat).Length;

                    List<string[]> rowData = new List<string[]>();
                    var row = new List<string>();
                    foreach (var key in systemBodyInfo.Minerals.Keys)
                    {
                        row.Clear();
                        var mineralData = _mineralDefinitions.FirstOrDefault(x => x.ID == key);
                        if (mineralData != null)
                        {
                            var mineralValues = systemBodyInfo.Minerals[key];

                            row.Add(mineralData.Name);
                            row.Add(mineralValues.Amount.ToString(_amountFormat));
                            row.Add(mineralValues.Accessibility.ToString("0.00"));
                            if (mineRates.Any())
                            {
                                var rate = Stringify.Quantity(mineRates.ContainsKey(key) ? mineRates[key] : 0) + "/day";
                                row.Add(rate);
                            }

                            rowData.Add(row.ToArray());
                        }
                    }

                    Helpers.RenderImgUITextTable(headerRow.ToArray(), rowData);
                }
            }
        }
    }
}*/
