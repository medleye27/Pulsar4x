using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Blueprints;
using Pulsar4X.Engine;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Industry;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Factions;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// If no component template is selected what should we show?
    /// </summary>
    public enum NoTemplateState
    {
        PleaseSelect,
        Created
    }

    public sealed class ComponentDesignDisplay
    {
        private static ComponentDesignDisplay? instance = null;
        private static readonly object padlock = new object();

        private NoTemplateState NoTemplateState = NoTemplateState.PleaseSelect;
        private ComponentDesigner? _componentDesigner;
        public ComponentTemplateBlueprint? Template { get; private set;}
        private string[]? _designTypes;
        private ComponentTemplateBlueprint[]? _designables;
        private static byte[] _nameInputBuffer = new byte[128];
        private static Tech[]? _techSDs;
        private static string[]? _techNames;
        private static int _techSelectedIndex = -1;
        //private TechSD[] _techSDs;
        private static string[]? _listNames;


        private ComponentDesignDisplay() { }

        internal static ComponentDesignDisplay GetInstance() {
            lock(padlock)
            {
                if(instance == null)
                {
                    instance = new ComponentDesignDisplay();
                }
            }

            return instance;
        }

        public void SetTemplate(ComponentTemplateBlueprint template, GlobalUIState state)
        {
            Template = template;

            var factionData = state.Faction.GetDataBlob<FactionInfoDB>().Data;
            var factionTech = state.Faction.GetDataBlob<FactionTechDB>();
            _componentDesigner = new ComponentDesigner(Template, factionData, factionTech);

            NoTemplateState = NoTemplateState.Created;
        }

        internal void Display(GlobalUIState uiState)
        {
            if(Template == null)
            {
                switch (NoTemplateState)
                {
                    case NoTemplateState.PleaseSelect:
                        DisplayPleaseSelectTemplate();
                        break;
                    case NoTemplateState.Created:
                        DisplayCreatedTemplate();
                        break;
                }
                return;
            }

            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignChildWindow", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), true))
            {
                DisplayHelpers.Header("Specifications",
                    "Configure the specifications for the component below.\n\n" +
                    "Different settings will determine the statistics and capabilities\n" +
                    "of the component.");

                GuiDesignUI(uiState); //Part design

                ImGui.EndChild();
            }
            ImGui.SameLine();
            ImGui.SetCursorPosY(27f);

            var position = ImGui.GetCursorPos();
            if (ImGui.BeginChild("ComponentDesignChildWindow2", new Vector2(windowContentSize.X * 0.49f, windowContentSize.Y * 0.65f), true))
            {
                GuiCostText(uiState); //Print cost

                ImGui.EndChild();
            }

            ImGui.SetCursorPos(new Vector2(position.X, position.Y + windowContentSize.Y * 0.662f));
            if (ImGui.BeginChild("ComponentDesignChildWindow3", new Vector2(windowContentSize.X * 0.49f, windowContentSize.Y * 0.34f), true))
            {
                var sizeAvailable = ImGui.GetContentRegionAvail();

                DisplayHelpers.Header("Finalize the Design");
                ImGui.Text("Name");
                ImGui.InputText("", _nameInputBuffer, 32);
                ImGui.SetCursorPosY(sizeAvailable.Y - 12f);
                if(ImGui.Button("Save", new Vector2(sizeAvailable.X, 0)))
                {
                    if(!_nameInputBuffer.All(b => b == 0))
                    {
                        if(_componentDesigner != null)
                        {
                            string name = ImGuiSDL2CSHelper.StringFromBytes(_nameInputBuffer);
                            _componentDesigner.Name = name;
                            _componentDesigner.CreateDesign(uiState.Faction);
                        }

                        //we reset the designer here, so we don't end up trying to edit the previous design.
                        var factionData = uiState.Faction.GetDataBlob<FactionInfoDB>().Data;
                        var factionTech = uiState.Faction.GetDataBlob<FactionTechDB>();
                        _componentDesigner = new ComponentDesigner(Template, factionData, factionTech);

                        NoTemplateState = NoTemplateState.Created;
                        Template = null;
                        _nameInputBuffer = new byte[128];
                    }
                }
                ImGui.EndChild();
            }
        }

        internal void GuiDesignUI(GlobalUIState uiState) //Creates all UI elements need for designing the Component
        {
            // FIXME: compact mode should be an option in the game settings?
            // if (ImGui.Button("Compact"))
            // {
            //     compactmod = !compactmod;
            // }

            //ImGui.NewLine();

            if (_componentDesigner != null) //Make sure comp is selected
            {
                foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes.Values) //For each property of the comp type
                {
                    ImGui.PushID(attribute.Name);

                    if (attribute.IsEnabled)
                    {
                        switch (attribute.GuiHint) //Either
                        {
                            case GuiHint.None:
                                break;
                            case GuiHint.GuiTechSelectionList: //Let the user pick a type from a list
                                GuiHintTechSelection(attribute, uiState);
                                break;
                            case GuiHint.GuiSelectionMaxMin: //Set a value
                                GuiHintMaxMin(attribute);
                                break;
                            case GuiHint.GuiSelectionMaxMinInt:
                                GuiHintMaxMinInt(attribute);
                                break;
                            case GuiHint.GuiTextDisplay: //Display a stat
                                //GuiHintText(attribute);
                                break;
                            case GuiHint.GuiEnumSelectionList: //Let the user pick a type from a hard coded list
                                GuiHintEnumSelection(attribute);
                                break;
                            case GuiHint.GuiOrdnanceSelectionList:
                                GuiHintOrdnanceSelection(attribute, uiState);
                                break;
                            case GuiHint.GuiTextSelectionFormula:
                                GuiHintTextSelectionFormula(attribute);
                                break;
                            case GuiHint.GuiFuelTypeSelection:
                                GuiHintFuelTypeSelection(attribute, uiState);
                                break;
                            case GuiHint.GuiTechCategorySelection:
                                GuiHintTechCategorySelection(attribute, uiState);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    ImGui.PopID();
                }

                ImGui.NewLine();
            }
            else //Tell the user they don't have a comp type selected
            {
                ImGui.NewLine();
                ImGui.Text("No component type selected");
                ImGui.NewLine();
            }
        }

        private void GuiCostText(GlobalUIState uiState) //Prints a 2 col table with the costs of the part
        {
            if (_componentDesigner != null) //If a part time is selected
            {
                DisplayHelpers.Header("Statistics");

                if(ImGui.BeginTable("DesignStatsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Attribute", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Type");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.ComponentType);

                    var activeMountTypes = _componentDesigner.GetActiveMountTypes();
                    if(activeMountTypes.Count > 0)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.SameLine();
                        ImGui.Text("Installs On or In");
                        ImGui.TableNextColumn();
                        for(int i = 0; i < activeMountTypes.Count; i++)
                        {
                            if(i < activeMountTypes.Count - 1)
                            {
                                ImGui.Text(activeMountTypes[i].ToDescription() +  ",");
                                ImGui.SameLine();
                            }
                            else
                            {
                                ImGui.Text(activeMountTypes[i].ToDescription());
                            }
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Mass");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Mass(_componentDesigner.MassValue));

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Volume");
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Volume(_componentDesigner.VolumeM3Value));

                    if(_componentDesigner.CrewReqValue > 0)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.SameLine();
                        ImGui.Text("Crew Required");
                        ImGui.TableNextColumn();
                        ImGui.Text(_componentDesigner.CrewReqValue.ToString(Styles.IntFormat));
                    }

                    foreach (ComponentDesignAttribute attribute in _componentDesigner.ComponentDesignAttributes.Values) //For each property of the comp type
                    {
                        if(attribute.IsEnabled && attribute.GuiHint == GuiHint.GuiTextDisplay)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text("");
                            ImGui.SameLine();
                            ImGui.Text(attribute.Name);
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(attribute.Description);
                            ImGui.TableNextColumn();

                            if(attribute.Unit.IsNotNullOrEmpty())
                            {
                                var value = attribute.Value;
                                var strUnit = attribute.Unit;
                                var displayStr = "";

                                switch (strUnit)
                                {
                                    case "KJ":
                                    {
                                        displayStr = Stringify.Energy(value);
                                        break;
                                    }
                                    case "KW":
                                    {
                                        displayStr = Stringify.Power(value);
                                        break;
                                    }
                                    case "m^2":
                                    {
                                        displayStr = Stringify.Volume(value);
                                        break;
                                    }
                                    case "nm":
                                    {
                                        displayStr = Stringify.DistanceSmall(value);
                                        break;
                                    }
                                    case "kg":
                                    {
                                        displayStr = Stringify.Mass(value);
                                        break;
                                    }
                                    case "m":
                                    {
                                        displayStr = Stringify.Distance(value);
                                        break;
                                    }
                                    case "N":
                                    {
                                        displayStr = Stringify.Thrust(value);
                                        break;
                                    }
                                    case "m/s":
                                        displayStr = Stringify.Velocity(value);
                                        break;
                                    default:
                                    {
                                        displayStr = attribute.Value.ToString(Styles.DecimalFormat) + " " + attribute.Unit;
                                        break;
                                    }
                                }

                                ImGui.Text(displayStr);
                                if(ImGui.IsItemHovered())
                                    ImGui.SetTooltip(attribute.Value.ToString(Styles.IntFormat) + " " + attribute.Unit);
                            }
                            else
                            {
                                ImGui.Text(attribute.Value.ToString(Styles.IntFormat));
                            }
                        }
                        else if(attribute.IsEnabled && attribute.GuiHint == GuiHint.GuiFuelTypeSelection)
                        {
                            var cargo = (ProcessedMaterial)uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetMaterial(attribute.ValueString);
                            ImGui.TableNextColumn();
                            ImGui.Text("");
                            ImGui.SameLine();
                            ImGui.Text("Fuel Type");
                            ImGui.TableNextColumn();
                            ImGui.Text(cargo.Name);
                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(cargo.Description);
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.NewLine();
                DisplayHelpers.Header("Costs");

                if(ImGui.BeginTable("DesignCostsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Cost");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.CreditCostValue.ToString(Styles.IntFormat));

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Research");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.ResearchCostValue.ToString(Styles.IntFormat) + " RP");

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text("Production");
                    ImGui.TableNextColumn();
                    ImGui.Text(_componentDesigner.IndustryPointCostsValue.ToString(Styles.IntFormat) + " IP");

                    ImGui.EndTable();
                }

                ImGui.NewLine();
                DisplayHelpers.Header("Resources Required");

                if(ImGui.BeginTable("DesignResourceCostsTables", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Resource", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Quantity Needed", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    foreach (var kvp in _componentDesigner.ResourceCostValues)
                    {
                        var resource = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(kvp.Key);
                        if (resource == null)
                            resource = (ICargoable)uiState.Faction.GetDataBlob<FactionInfoDB>().IndustryDesigns[kvp.Key];

                        ImGui.TableNextColumn();
                        ImGui.Text("");
                        ImGui.SameLine();
                        ImGui.Text(resource.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(kvp.Value.ToString(Styles.IntFormat));
                    }
                    ImGui.EndTable();
                }
            }
        }

        private void GuiHintText(ComponentDesignAttribute attribute)
        {
            var value = attribute.Value;
            var strUnit = attribute.Unit;
            var displayStr = "";
            switch (strUnit)
            {
                case "KJ":
                {
                    displayStr = Stringify.Energy(value);
                    break;
                }
                default:
                {
                    displayStr = attribute.Value.ToString() + " " + attribute.Unit;
                    break;
                }


            }

            Title(attribute.Name, displayStr);
        }

        private void GuiHintMaxMin(ComponentDesignAttribute attribute)
        {
            Title(attribute.Name, attribute.Description);

            attribute.SetMax();
            attribute.SetMin();
            //attribute.SetValue();
            attribute.SetStep();

            var max = attribute.MaxValue;
            var min = attribute.MinValue;
            double val = attribute.Value;
            double step = attribute.StepValue;
            double fstep = step * 10;
            IntPtr valPtr;
            IntPtr maxPtr;
            IntPtr minPtr;
            IntPtr stepPtr;
            IntPtr fstepPtr;

            unsafe
            {
                valPtr = new IntPtr(&val);
                maxPtr = new IntPtr(&max);
                minPtr = new IntPtr(&min);
                stepPtr = new IntPtr(&step);
                fstepPtr = new IntPtr(&fstep);
            }

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.SliderScalar("##scaler" + attribute.Name, ImGuiDataType.Double, valPtr, minPtr, maxPtr))
            {
                attribute.SetValueFromInput(val);
            }
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.InputScalar("##input" + attribute.Name, ImGuiDataType.Double, valPtr, stepPtr, fstepPtr))
                attribute.SetValueFromInput(val);
            ImGui.NewLine();
        }

        private void GuiHintMaxMinInt(ComponentDesignAttribute attribute)
        {
            Title(attribute.Name, attribute.Description);

            attribute.SetMax();
            attribute.SetMin();
            //attribute.SetValue();
            attribute.SetStep();

            var max = attribute.MaxValue;
            var min = attribute.MinValue;
            int val = (int)attribute.Value;
            double step = attribute.StepValue;
            double fstep = step * 10;

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if(ImGui.SliderInt("##scaler" + attribute.Name, ref val, (int)min, (int)max))
            {
                attribute.SetValueFromInput(val);
            }

            ImGui.SetNextItemWidth(sizeAvailable.X);
            if(ImGui.InputInt("##input" + attribute.Name, ref val, (int)step, (int)fstep))
            {
                attribute.SetValueFromInput(val);
            }
            ImGui.NewLine();
        }

        private void GuiHintTechSelection(ComponentDesignAttribute attribute, GlobalUIState uiState)
        {
            Title(attribute.Name, attribute.Description);

            int i = 0;
            _techSDs = new Tech[attribute.GuidDictionary.Count];
            _techNames = new string[attribute.GuidDictionary.Count];
            foreach (var kvp in attribute.GuidDictionary)
            {
                Tech sd = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.Techs[(string)kvp.Key];
                _techSDs[i] = sd;
                _techNames[i] = sd.Name;
                i++;
            }

            ImGui.TextWrapped(attribute.Value.ToString());

            if (ImGui.Combo("Select Tech", ref _techSelectedIndex, _techNames, _techNames.Length))
            {
                attribute.SetValueFromString(_techSDs[_techSelectedIndex].UniqueID);
            }

            ImGui.NewLine();
        }

        private void GuiHintEnumSelection(ComponentDesignAttribute attribute)
        {
            _listNames = Enum.GetNames(attribute.EnumType);

            Title(attribute.Name, attribute.Description);

            int listCount = Math.Min((int)attribute.MaxValue, _listNames.Length);
            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref attribute.ListSelection, _listNames, listCount))
            {
                int enumVal = (int)Enum.Parse(attribute.EnumType, _listNames[attribute.ListSelection]);
                attribute.SetValueFromInput(enumVal);
            }

            ImGui.NewLine();
        }

        private void GuiHintOrdnanceSelection(ComponentDesignAttribute attribute, GlobalUIState uiState)
        {
            var dict = uiState.Faction.GetDataBlob<FactionInfoDB>().MissileDesigns;
            _listNames = new string[dict.Count];
            OrdnanceDesign[] ordnances = new OrdnanceDesign[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                _listNames[i] = kvp.Value.Name;
                ordnances[i] = kvp.Value;
            }

            Title(attribute.Name, attribute.Description);

            ImGui.TextWrapped(attribute.Value.ToString());

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref attribute.ListSelection, _listNames, _listNames.Length))
            {
                attribute.SetValueFromString(ordnances[attribute.ListSelection].UniqueID);
            }

            ImGui.NewLine();
        }

        private void GuiHintFuelTypeSelection(ComponentDesignAttribute attribute, GlobalUIState uiState)
        {
            var cargoTypesToDisplay = new Dictionary<int, ICargoable>();
            var keys = new List<int>();
            var names = new List<string>();

            foreach(string cargoType in attribute.GuidDictionary.Keys)
            {
                var fuelType = attribute.GuidDictionary[cargoType].StrResult;
                string cargoTypeID = cargoType.ToString();
                var cargos = uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAll().Where(c => c.Value.CargoTypeID.Equals(cargoTypeID));
                foreach(var cargo in cargos)
                {
                    if(cargo.Value is ProcessedMaterial
                        && ((ProcessedMaterial)cargo.Value).Formulas != null
                        && ((ProcessedMaterial)cargo.Value).Formulas.ContainsKey("ExhaustVelocity")
                        && ((ProcessedMaterial)cargo.Value).Formulas["ExhaustVelocity"].IsNotNullOrEmpty()
                        && ((ProcessedMaterial)cargo.Value).Formulas.ContainsKey("FuelType")
                        && ((ProcessedMaterial)cargo.Value).Formulas["FuelType"] == fuelType)
                    {
                        cargoTypesToDisplay.Add(cargo.Key, cargo.Value);
                        keys.Add(cargo.Key);
                        names.Add(cargo.Value.Name);
                    }
                }
            }

            string[] arrayNames = names.ToArray();

            Title(attribute.Name, attribute.Description);

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if(ImGui.Combo("###cargotypeselection", ref attribute.ListSelection, arrayNames, arrayNames.Length))
            {
                attribute.SetValueFromString(cargoTypesToDisplay[keys[attribute.ListSelection]].UniqueID);
            }
        }

        private void GuiHintTextSelectionFormula(ComponentDesignAttribute attribute)
        {
            _listNames = new string[attribute.GuidDictionary.Count];

            int i = 0;
            foreach (var kvp in attribute.GuidDictionary)
            {
                _listNames[i] = (string)kvp.Key;
                i++;
            }

            Title(attribute.Name, attribute.Description);

            ImGui.TextWrapped(attribute.Value.ToString());

            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref attribute.ListSelection, _listNames, _listNames.Length))
            {
                var key = _listNames[attribute.ListSelection];
                var value = attribute.GuidDictionary[key];
                attribute.SetValueFromDictionaryExpression(_listNames[attribute.ListSelection]);
            }
        }

        private void GuiHintTechCategorySelection(ComponentDesignAttribute attribute, GlobalUIState uiState)
        {
            _listNames = new string[uiState.Game.TechCategories.Count];

            int i = 0;
            foreach (var kvp in uiState.Game.TechCategories)
            {
                _listNames[i] = (string)kvp.Value.Name;
                i++;
            }

            Title(attribute.Name, attribute.Description);
            var sizeAvailable = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(sizeAvailable.X);
            if (ImGui.Combo("###Select", ref attribute.ListSelection, _listNames, _listNames.Length))
            {
                var name = _listNames[attribute.ListSelection];
                var value = uiState.Game.TechCategories.Where(c => c.Value.Name.Equals(name)).First();
                attribute.SetValueFromString(value.Key);
            }
        }

        private void DisplayPleaseSelectTemplate()
        {
            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignSelectTemplate", windowContentSize, false))
            {
                string message = "Please select a template on the left.";
                var size = ImGui.GetContentRegionAvail();
                var textSize = ImGui.CalcTextSize(message);
                ImGui.SetCursorPos(new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2));
                ImGui.Text(message);
                ImGui.EndChild();
            }
        }

        private void DisplayCreatedTemplate()
        {
            var windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ComponentDesignCreated", windowContentSize, false))
            {
                string message = "Design has been created, it will now be availble to Research.";
                var size = ImGui.GetContentRegionAvail();
                var textSize = ImGui.CalcTextSize(message);
                ImGui.SetCursorPos(new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2));
                ImGui.Text(message);
                ImGui.EndChild();
            }
        }

        private void Title(string title, string tooltip)
        {
            ImGui.Text(title);

            if(tooltip.IsNullOrEmpty()) return;

            ImGui.SameLine();
            ImGui.Text("[?]");
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }
    }
}


