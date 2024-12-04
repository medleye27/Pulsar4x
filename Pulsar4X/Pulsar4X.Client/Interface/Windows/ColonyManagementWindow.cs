using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Colonies;
using Pulsar4X.People;

namespace Pulsar4X.SDL2UI
{
    public class ColonyManagementWindow : PulsarGuiWindow
    {
        private Dictionary<string, bool> isExpanded = new();
        public EntityState? SelectedEntity { get; private set; } = null;

        internal static ColonyManagementWindow GetInstance()
        {
            ColonyManagementWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ColonyManagementWindow)))
            {
                thisitem = new ColonyManagementWindow()
                {
                    SelectedEntity = null
                };
            }
            thisitem = (ColonyManagementWindow)_uiState.LoadedWindows[typeof(ColonyManagementWindow)];

            return thisitem;
        }

        public void SelectEntity(EntityState entityState)
        {
            SelectedEntity = entityState;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(Window.Begin("Manage Colonies", ref IsActive))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                if(ImGui.BeginChild("Colonies", new Vector2(Styles.LeftColumnWidth, windowContentSize.Y), true))
                {
                    DisplayHelpers.Header("Select Colony to Manage");
                    foreach(var (id, systemState) in _uiState.StarSystemStates)
                    {
                        if(!isExpanded.ContainsKey(id)) isExpanded.Add(id, true);
                        ImGui.SetNextItemOpen(isExpanded[id], ImGuiCond.Appearing);
                        if(ImGui.TreeNode(systemState.StarSystem.NameDB.DefaultName))
                        {
                            foreach(var (c_id, colony) in systemState.EntityStatesColonies)
                            {
                                var population = colony.Entity.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();

                                if(SelectedEntity == colony)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.75f, 0.25f, 0.25f, 1f));
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0f));
                                }

                                if(ImGui.SmallButton(colony.Name + " (" + Stringify.Quantity(population) + ")"))
                                {
                                    SelectEntity(colony);
                                }
                                ImGui.PopStyleColor();
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.EndChild();
                }

                if(SelectedEntity == null) return;

                ImGui.SameLine();

                if(ImGui.BeginChild("ColoniesTabs"))
                {
                    ImGui.BeginTabBar("EconomicsTabBar", ImGuiTabBarFlags.None);

                    if(ImGui.BeginTabItem("Summary"))
                    {
                        SelectedEntity.Entity.DisplaySummary(SelectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Production"))
                    {
                        SelectedEntity.Entity.DisplayIndustry(SelectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Mining"))
                    {
                        SelectedEntity.Entity.DisplayMining(_uiState);
                        ImGui.EndTabItem();
                    }
                    // if(ImGui.BeginTabItem("Logistics"))
                    // {
                    //     SelectedEntity.Entity.DisplayLogistics(SelectedEntity, _uiState);
                    //     ImGui.EndTabItem();
                    // }
                    if(SelectedEntity.Entity.HasDataBlob<NavalAcademyDB>() && ImGui.BeginTabItem("Naval Academy"))
                    {
                        SelectedEntity.Entity.DisplayNavalAcademy(SelectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                    ImGui.EndChild();
                }

                Window.End();
            }
        }
    }
}