using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GameEngine.WarpMove;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Components;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.DataStructures;
using Pulsar4X.Atb;
using Pulsar4X.Energy;
using Pulsar4X.Factions;

namespace Pulsar4X.SDL2UI
{
    public class ShipDesignWindow : PulsarGuiWindow
    {
        private bool ShowNoDesigns = false;
        private byte[] SelectedDesignName =  ImGuiSDL2CSHelper.BytesFromString("foo", 32);
        private SafeList<ShipDesign> ExistingShipDesigns = new();
        private string SelectedExistingDesignID = String.Empty;
        private bool SelectedDesignObsolete;
        bool _imagecreated = false;

        private List<ComponentDesign> AvailableShipComponents = new();
        private List<ComponentDesign> AllShipComponents = new();
        private static string[]? _sortedComponentNames;
        private int _componentFilterIndex = 0;

        List<(ComponentDesign design, int count)> SelectedComponents = new List<(ComponentDesign design, int count)>();

        private IntPtr _shipImgPtr;

        //TODO: armor, temporary, maybe density should be an "equvelent" and have a different mass? (damage calcs use density for penetration)
        List<ArmorBlueprint> _armorSelection = new List<ArmorBlueprint>();
        private string[]? _armorNames;
        private int _armorIndex = 0;
        private float _armorThickness = 10;
        private ArmorBlueprint? _armor;
        private double _armorMass = 0;

        private int rawimagewidth;
        private int rawimageheight;

        private double _massDry;
        private double _massWet;
        private double _ttwr;
        private double _dv;
        private double _wspd;
        private double _wcc;
        private double _wsc;
        private double _wec;
        private double _tn;
        private double _estor;
        private double _egen;
        private double _fuelStoreMass;
        private double _fuelStoreVolume;
        private double _grossTonnage;
        private ICargoable? _fuelType;
        bool displayimage = true;
        private EntityDamageProfileDB? _profile;
        private bool existingdesignsstatus = true;
        bool DesignChanged = false;

        private FactionInfoDB _factionInfoDB;

        private ShipDesignWindow()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
            _factionInfoDB = _uiState.Faction.GetDataBlob<FactionInfoDB>();

            RefreshComponentDesigns();
            RefreshArmor();
            RefreshExistingClasses();
        }

        public override void OnSystemTickChange(DateTime newDateTime)
        {
            RefreshComponentDesigns();
            RefreshExistingClasses();
        }

        internal static ShipDesignWindow GetInstance()
        {
            ShipDesignWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ShipDesignWindow)))
            {
                thisitem = new ShipDesignWindow();
                thisitem.RefreshComponentDesigns();
                thisitem.RefreshExistingClasses();
            }
            else
                thisitem = (ShipDesignWindow)_uiState.LoadedWindows[typeof(ShipDesignWindow)];

            return thisitem;
        }

        void RefreshComponentDesigns()
        {
            AllShipComponents = _factionInfoDB.ComponentDesigns.Values.ToList();
            AllShipComponents.Sort((a, b) => a.Name.CompareTo(b.Name));

            var templatesByGroup = AllShipComponents.GroupBy(t => t.ComponentType);
            var groupNames = templatesByGroup.Select(g => g.Key).ToList();
            var sortedTempGroupNames = groupNames.OrderBy(name => name).ToArray();
            _sortedComponentNames = new string[sortedTempGroupNames.Length + 1];
            _sortedComponentNames[0] = "All";
            Array.Copy(sortedTempGroupNames, 0, _sortedComponentNames, 1, sortedTempGroupNames.Length);

            if(_componentFilterIndex == 0)
            {
                AvailableShipComponents = new List<ComponentDesign>(AllShipComponents);
            }
            else
            {
                AvailableShipComponents = AllShipComponents.Where(t => t.ComponentType.Equals(_sortedComponentNames[_componentFilterIndex])).ToList();
            }
        }

        void RefreshExistingClasses()
        {
            var designs = _factionInfoDB.ShipDesigns.Values.Where(d => !d.IsObsolete).ToList();
            designs.Sort((a, b) => a.Name.CompareTo(b.Name));
            ExistingShipDesigns = new SafeList<ShipDesign>(designs);

            if(ExistingShipDesigns.Count == 0)
            {
                ShowNoDesigns = true;
                return;
            }
            if(SelectedExistingDesignID.IsNullOrEmpty() && ExistingShipDesigns.Count > 0)
                Select(ExistingShipDesigns[0]);

            ShowNoDesigns = false;
        }

        void RefreshArmor()
        {
            var factionData = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data;
            _armorNames = new string[factionData.Armor.Count];
            int i = 0;
            foreach (var kvp in factionData.Armor)
            {
                var armorMat = factionData.CargoGoods.GetAny(kvp.Value.ResourceID);
                _armorSelection.Add(kvp.Value);

                _armorNames[i]= armorMat?.Name ?? "Unknown";
                i++;
            }
            //TODO: bleed over from mod data to get a default armor...
            _armor = factionData.Armor["plastic-armor"];
            _armorThickness = 3;
        }

        void Select(ShipDesign design)
        {
            SelectedExistingDesignID = design.UniqueID;
            SelectedDesignName = ImGuiSDL2CSHelper.BytesFromString(design.Name, 32);
            SelectedComponents = design.Components;
            SelectedDesignObsolete = design.IsObsolete;
            _armor = design.Armor.type;
            _armorIndex = _armorSelection.IndexOf(_armor);
            _armorThickness = design.Armor.thickness;
            DesignChanged = true;
            UpdateShipStats();
        }

        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ship Design", ref IsActive, _flags))
            {
                if(ExistingShipDesigns.Count != _uiState.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.Count)
                {
                    RefreshExistingClasses();
                }
                if (AllShipComponents.Count != _uiState.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.Count)
                {
                    RefreshComponentDesigns();
                }

                DisplayExistingDesigns();
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);

                if(ShowNoDesigns)
                {
                    ImGui.Text("Create a new design to begin editing.");
                    return;
                }

                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
                var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
                if(ImGui.BeginChild("ShipDesign1", firstChildSize, true))
                {
                    DisplayComponentSelection();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);
                if(ImGui.BeginChild("ShipDesign2", secondChildSize, true))
                {
                    DisplayComponents();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);
                if(ImGui.BeginChild("ShipDesign3", thirdChildSize, true))
                {
                    DisplayStats();
                    ImGui.EndChild();
                }
            }
        }

        internal void NewShipButton()
        {
            if (ImGui.Button("Save Design"))
            {
                int version = 0;
                var name = ImGuiSDL2CSHelper.StringFromBytes(SelectedDesignName);

                if(name.IsNotNullOrEmpty())
                {
                    foreach (var shipclass in ExistingShipDesigns)
                    {
                        if (shipclass.Name.Equals(name))
                        {
                            if (shipclass.DesignVersion >= version)
                                version = shipclass.DesignVersion + 1;
                        }
                    }
                    var design = _factionInfoDB.ShipDesigns[SelectedExistingDesignID];
                    design.Name = name;
                    design.Components = SelectedComponents;
                    if(_armor == null)
                        throw new NullReferenceException();

                    design.Armor = (_armor, _armorThickness);
                    design.IsObsolete = SelectedDesignObsolete;
                    if(design.IsObsolete)
                    {
                        // If the design is obsolete mark it is invalid so it can't be produced
                        design.IsValid = false;
                    }
                    else
                    {
                        design.IsValid = IsDesignValid();
                    }

                    if(design.IsObsolete)
                    {
                        SelectedExistingDesignID = String.Empty;
                    }

                    RefreshExistingClasses();
                    // var shipDesign = new ShipDesign(_uiState.Faction.GetDataBlob<FactionInfoDB>(), name, SelectedComponents, (_armor, _armorThickness))
                    // {
                    //     DesignVersion = version
                    // };
                }
            }
        }

        internal void DisplayExistingDesigns()
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("ComponentDesignSelection", new Vector2(Styles.LeftColumnWidth, windowContentSize.Y - 24f), true))
            {
                DisplayHelpers.Header("Existing Designs", "Select an existing ship design to edit it.");

                foreach(var design in ExistingShipDesigns)
                {
                    string name = design.Name;
                    if (ImGui.Selectable(name + "###existing-design-" + design.UniqueID, design.UniqueID.Equals(SelectedExistingDesignID)))
                    {
                        Select(design);
                    }
                    if(ImGui.BeginPopupContextItem())
                    {
                        if(ImGui.MenuItem("Delete###delete-" + design.UniqueID))
                        {
                            _factionInfoDB.ShipDesigns.Remove(design.UniqueID);
                            SelectedExistingDesignID = String.Empty;
                            RefreshExistingClasses();
                        }
                        ImGui.EndPopup();
                    }
                }
                ImGui.EndChild();
            }

            if(ImGui.Button("Create New Design", new Vector2(204f, 0f)))
            {
                string originalName = "auto-gen names pls", name = originalName;
                int counter = 1;
                while(_factionInfoDB.ShipDesigns.Values.Any(d => d.Name.Equals(name)))
                {
                    name = originalName + " " + counter.ToString();
                    counter++;
                }
                SelectedDesignName = ImGuiSDL2CSHelper.BytesFromString(name);
                SelectedComponents = new List<(ComponentDesign design, int count)>();
                GenImage();
                RefreshArmor();
                DesignChanged = true;

                if(_armor == null)
                    throw new NullReferenceException();

                ShipDesign design = new(_factionInfoDB, name, SelectedComponents, (_armor, _armorThickness))
                {
                    IsValid = false
                };
                RefreshExistingClasses();
                SelectedExistingDesignID = design.UniqueID;
            }
        }

        internal void DisplayComponents()
        {
            DisplayHelpers.Header("Current Design");

            if(SelectedComponents.Count == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                ImGui.Text("Add components from the available components list");
                ImGui.PopStyleColor();
            }
            else
            {
                DisplayComponentsTable();
            }

            ImGui.NewLine();
            DisplayHelpers.Header("Armor");
            if(ImGui.BeginTable("CurrentShipDesignTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("Type");
                ImGui.TableNextColumn();

                if(_armorNames == null)
                    throw new NullReferenceException();

                if (ImGui.Combo("##Armor Selection", ref _armorIndex, _armorNames, _armorNames.Length))
                {
                    _armor = _armorSelection[_armorIndex];
                    DesignChanged = true;
                    ImGui.EndCombo();
                }

                ImGui.TableNextColumn();
                ImGui.Text("Density");
                ImGui.TableNextColumn();
                ImGui.Text(_armorSelection[_armorIndex].Density.ToString());

                ImGui.TableNextColumn();
                ImGui.Text("Thickness");
                ImGui.TableNextColumn();
                ImGui.Text(_armorThickness.ToString());

                ImGui.SameLine();
                if (ImGui.SmallButton("+##armor")) //todo: imagebutton
                {
                    _armorThickness++;
                    DesignChanged = true;
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("-##armor") && _armorThickness > 0) //todo: imagebutton
                {
                    _armorThickness--;
                    DesignChanged = true;
                }

                ImGui.TableNextColumn();
                ImGui.Text("Mass");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_armorMass));

                ImGui.SameLine();
                ImGui.EndTable();
            }
        }

        internal void DisplayComponentsTable()
        {
            if(ImGui.BeginTable("CurrentShipDesignTable", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                int selectedItem = -1;
                for (int i = 0; i < SelectedComponents.Count; i++)
                {
                    string name = SelectedComponents[i].design.Name;
                    int number = SelectedComponents[i].count;

                    ImGui.TableNextColumn();
                    ImGui.Text(name);

                    bool hovered = ImGui.IsItemHovered();
                    if (hovered)
                    {
                        selectedItem = i;
                        DisplayHelpers.DescriptiveTooltip(SelectedComponents[i].design.Name, SelectedComponents[i].design.TypeName, SelectedComponents[i].design.Description);
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(number.ToString());

                    ImGui.SameLine();
                    if (ImGui.SmallButton("+##" + i)) //todo: imagebutton
                    {
                        SelectedComponents[i] = (SelectedComponents[i].design, SelectedComponents[i].count + 1);
                        DesignChanged = true;
                    }
                    ImGui.SameLine();
                    if (ImGui.SmallButton("-##" + i) && number > 0) //todo: imagebutton
                    {
                        SelectedComponents[i] = (SelectedComponents[i].design, SelectedComponents[i].count - 1);
                        DesignChanged = true;
                    }
                    ImGui.TableNextColumn();
                    if (ImGui.SmallButton("x##" + i)) //todo: imagebutton
                    {
                        SelectedComponents.RemoveAt(i);
                        DesignChanged = true;
                    }

                    if (i > 0)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("^##" + i)) //todo: imagebutton
                        {

                            (ComponentDesign design, int count) item = SelectedComponents[i];
                            SelectedComponents.RemoveAt(i);
                            SelectedComponents.Insert(i - 1, item);

                            DesignChanged = true;
                        }
                    }
                    if (i < SelectedComponents.Count - 1)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("v##" + i)) //todo: imagebutton
                        {
                            (ComponentDesign design, int count) item = SelectedComponents[i];
                            SelectedComponents.RemoveAt(i);
                            SelectedComponents.Insert(i + 1, item);
                            DesignChanged = true;
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        internal void DisplayComponentSelection()
        {
            if(_sortedComponentNames == null)
                throw new NullReferenceException();

            DisplayHelpers.Header("Available Components");

            var availableSize = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(availableSize.X);
            if(ImGui.Combo("###component-filter", ref _componentFilterIndex, _sortedComponentNames, _sortedComponentNames.Length))
            {
                if(_componentFilterIndex == 0)
                {
                    AvailableShipComponents = new List<ComponentDesign>(AllShipComponents);
                }
                else
                {
                    AvailableShipComponents = AllShipComponents.Where(t => t.ComponentType.Equals(_sortedComponentNames[_componentFilterIndex])).ToList();
                }
                ImGui.EndCombo();
            }

            if(ImGui.BeginTable("DesignStatsTables", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 2f);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, .6f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < AvailableShipComponents.Count; i++)
                {
                    if(!AvailableShipComponents[i].ComponentMountType.HasFlag(ComponentMountType.ShipComponent))
                        continue;

                    var design = AvailableShipComponents[i];
                    string name = design.Name;

                    ImGui.TableNextColumn();
                    ImGui.Text(name);
                    if(ImGui.IsItemHovered())
                    {
                        void TooltipExtension()
                        {
                            ImGui.Text("Mass: " + Stringify.Mass(AvailableShipComponents[i].MassPerUnit));
                            ImGui.Text("Volume: " + Stringify.Volume(AvailableShipComponents[i].VolumePerUnit));
                            ImGui.Text("Crew Required: " + AvailableShipComponents[i].CrewReq);
                        }

                        DisplayHelpers.DescriptiveTooltip(AvailableShipComponents[i].Name, AvailableShipComponents[i].TypeName, AvailableShipComponents[i].Description, TooltipExtension);
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(design.ComponentType);
                    ImGui.TableNextColumn();
                    ImGui.InvisibleButton("", new Vector2(4, 8));
                    ImGui.SameLine();
                    if(ImGui.SmallButton("+ Add###add-component-" + i))
                    {
                        SelectedComponents.Add((AvailableShipComponents[i], 1));
                        DesignChanged = true;
                    }
                }

                ImGui.EndTable();
            }
        }

        internal void GenImage()
        {
            if(_profile == null)
                throw new NullReferenceException();

            _shipImgPtr = SDL2Helper.CreateSDLTexture(_uiState.rendererPtr, _profile.DamageProfile, _imagecreated);
            rawimagewidth = _profile.DamageProfile.Width;
            rawimageheight = _profile.DamageProfile.Height;
            _imagecreated = true;
        }

        internal void DisplayStats()
        {
            DisplayHelpers.Header("Statisitcs", "The attributes of the ship are calculated based on the components you have added to the design.");

            UpdateShipStats();
            if(ImGui.BeginTable("DesignStatsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("Gross Tonnage");
                ImGui.TableNextColumn();
                ImGui.Text(_grossTonnage.ToString(Styles.IntFormat));

                ImGui.TableNextColumn();
                ImGui.Text("Mass (Dry)");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_massDry));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Wet: " + Stringify.Mass(_massDry + _fuelStoreMass));
                }

                ImGui.TableNextColumn();
                ImGui.Text("Total Thrust");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Thrust(_tn));

                ImGui.TableNextColumn();
                ImGui.Text("Thrust to Mass Ratio");
                ImGui.TableNextColumn();
                ImGui.Text(_ttwr.ToString(Styles.DecimalFormat));

                ImGui.TableNextColumn();
                var fuelName = _fuelType?.Name ?? "Unknown";
                ImGui.Text("Fuel Capacity (" + fuelName + ")");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Mass(_fuelStoreMass));
                ImGui.SameLine();
                ImGui.Text(Stringify.Volume(_fuelStoreVolume));

                ImGui.TableNextColumn();
                ImGui.Text("Delta V");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Velocity(_dv));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Speed");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Velocity(_wspd));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Bubble Creation");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_wcc));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Bubble Sustain");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_wsc));

                ImGui.TableNextColumn();
                ImGui.Text("Warp Bubble Collapse");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_wec));

                ImGui.TableNextColumn();
                ImGui.Text("Energy Output");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Power(_egen));

                ImGui.TableNextColumn();
                ImGui.Text("Energy Storage");
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Energy(_estor));

                ImGui.EndTable();
            }

            ImGui.NewLine();
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Details");
            ImGui.PopStyleColor();
            ImGui.Separator();

            ImGui.Text("Design Name:");
            ImGui.InputText("###Design Name", SelectedDesignName, (uint)SelectedDesignName.Length);
            ImGui.NewLine();
            ImGui.Text("Is Obsolete?");
            ImGui.Checkbox("###IsObsolete", ref SelectedDesignObsolete);

            if(!IsDesignValid())
            {
                ImGui.NewLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                ImGui.Text("Current design is invalid!");
                // TODO: tell the player what is invalid about their design
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("You will not be able to construct ships with an invalid design.");
                ImGui.PopStyleColor();
            }

            ImGui.NewLine();
            NewShipButton();
            ImGui.SameLine();
            ImGui.Checkbox("Show Pic", ref displayimage);
            ImGui.NewLine();

            var size = ImGui.GetContentRegionAvail();
            DisplayImage(size.X, size.Y);

            ImGui.EndChild();
        }

        private void UpdateShipStats()
        {
            if(!DesignChanged) return;

            if(_armor == null)
                throw new NullReferenceException();

            _profile = new EntityDamageProfileDB(SelectedComponents, (_armor, _armorThickness));
            if(displayimage)
            {
                GenImage();
            }

            long mass = 0;
            double fu = 0;
            double tn = 0;
            double ev = 0;

            double wp = 0;
            double wcc = 0;
            double wsc = 0;
            double wec = 0;
            double egen = 0;
            double estor = 0;
            string thrusterFuel = String.Empty;
            Dictionary<string, double> cstore = new Dictionary<string, double>();

            double volume = 0;

            foreach (var component in SelectedComponents)
            {
                mass += component.design.MassPerUnit * component.count;
                volume += component.design.VolumePerUnit * component.count;
                if (component.design.HasAttribute<NewtonionThrustAtb>())
                {
                    var atb = component.design.GetAttribute<NewtonionThrustAtb>();
                    ev = atb.ExhaustVelocity;
                    fu += atb.FuelBurnRate * component.count;
                    tn += ev * atb.FuelBurnRate * component.count;
                    thrusterFuel = atb.FuelType;
                }

                if (component.design.HasAttribute<WarpDriveAtb>())
                {
                    var atb = component.design.GetAttribute<WarpDriveAtb>();
                    wp += atb.WarpPower * component.count;
                    wcc += atb.BubbleCreationCost * component.count;
                    wsc += atb.BubbleSustainCost * component.count;
                    wec += atb.BubbleCollapseCost * component.count;

                }

                if (component.design.HasAttribute<EnergyGenerationAtb>())
                {
                    var atb = component.design.GetAttribute<EnergyGenerationAtb>();
                    egen += atb.PowerOutputMax * component.count;

                }

                if (component.design.HasAttribute<EnergyStoreAtb>())
                {
                    var atb = component.design.GetAttribute<EnergyStoreAtb>();
                    estor += atb.MaxStore * component.count;
                }

                if (component.design.HasAttribute<VolumeStorageAtb>())
                {
                    var atb = component.design.GetAttribute<VolumeStorageAtb>();
                    var typeid = atb.StoreTypeID;
                    var amount = atb.MaxVolume * component.count;
                    if (!cstore.ContainsKey(typeid))
                        cstore.Add(typeid, amount);
                    else
                        cstore[typeid] += amount;

                }
            }

            _armorMass = ShipDesign.GetArmorMass(_profile, _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods);
            mass += (long)Math.Round(_armorMass);

            var K = 0.2 + 0.02 * Math.Log10(volume);

            _grossTonnage = volume * K; // GT = V * K from: https://en.wikipedia.org/wiki/Gross_tonnage
            _massDry = mass;
            _tn = tn;
            _ttwr = (tn / mass) * 0.01;
            _wcc = wcc;
            _wec = wec;
            _wsc = wsc;
            _wspd = WarpMath.MaxSpeedCalc(wp, mass);
            _egen = egen;
            _estor = estor;
            //double fuelMass = 0;
            if (thrusterFuel.IsNotNullOrEmpty())
            {
                _fuelType = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(thrusterFuel);
                if (_fuelType != null && cstore.ContainsKey(_fuelType.CargoTypeID))
                {
                    _fuelStoreVolume = cstore[_fuelType.CargoTypeID];
                    var fuelDensity = _fuelType.MassPerUnit / _fuelType.VolumePerUnit;
                    _fuelStoreMass = _fuelStoreVolume * fuelDensity;

                }
            }

            _massWet = _massDry + _fuelStoreMass;
            _dv = OrbitMath.TsiolkovskyRocketEquation(_massWet, _massDry, ev);

            DesignChanged = false;
        }

        private bool IsDesignValid()
        {
            return _massDry > 0 &&
                    _tn > 0 &&
                    _ttwr > 0 &&
                    _egen > 0 &&
                    _estor > 0;
        }

        internal bool CheckDisplayImage(float maxwidth, float maxheight, float checkwidth)
        {
            if (_shipImgPtr != IntPtr.Zero && displayimage)
            {

                maxwidth = ImGui.GetWindowWidth();// ImGui.GetColumnWidth();;//
                int maxheightint = (int)(maxheight / 4);
                maxheight = maxheightint * 4;//ImGui.GetWindowHeight() * _imageratio;
                float scalew = 1;
                float scaleh = 1;
                float scale;
                scalew = maxwidth / rawimagewidth;
                scaleh = maxheight / rawimageheight;

                scale = Math.Min(scaleh, scalew);

                if (rawimagewidth * scale < checkwidth)
                {
                    return true;
                }
            }
            return false;
        }

        internal void DisplayImage(float maxwidth, float maxheight)
        {
            if (_shipImgPtr != IntPtr.Zero && displayimage)
            {
                int maxheightint = (int)(maxheight / 4);
                maxheight = maxheightint*4;//ImGui.GetWindowHeight() * _imageratio;
                float scalew = 1;
                float scaleh = 1;
                float scale;

                scalew = maxwidth / rawimagewidth;
                scaleh = maxheight / rawimageheight;

                scale = Math.Min(scaleh, scalew);

                ImGui.Image(_shipImgPtr, new System.Numerics.Vector2(rawimagewidth * scale, rawimageheight * scale));
            }
        }
    }
}