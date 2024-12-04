using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.People;

namespace Pulsar4X.SDL2UI
{
    public class CommanderWindow : PulsarGuiWindow
    {
        private Entity _faction;
        private FactionInfoDB _factionInfoDB;
        private Dictionary<int, Entity> _commanders;
        private CommanderWindow()
        {
            _faction = _uiState.Faction;
            _factionInfoDB = _faction.GetDataBlob<FactionInfoDB>();
            _commanders = new Dictionary<int, Entity>();
        }

        internal static CommanderWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(CommanderWindow)))
            {
                return new CommanderWindow();
            }
            return (CommanderWindow)_uiState.LoadedWindows[typeof(CommanderWindow)];
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(Window.Begin("Commanders", ref IsActive, _flags))
            {
                if(ImGui.BeginTable("CommanderTable", 3, Styles.TableFlags))
                {
                    ImGui.TableSetupColumn("Commander");
                    ImGui.TableSetupColumn("Yrs of Service");
                    ImGui.TableSetupColumn("Yrs in Rank");
                    ImGui.TableHeadersRow();

                    foreach(var commanderID in _factionInfoDB.Commanders)
                    {
                        Entity commander;
                        if(_commanders.ContainsKey(commanderID))
                        {
                            commander = _commanders[commanderID];
                        }
                        else
                        {
                            commander = _uiState.Game.GlobalManager.GetGlobalEntityById(commanderID);
                            _commanders.Add(commanderID, commander);
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text(commander.GetName(_faction.Id));
                        ImGui.TableNextColumn();
                        var commanderDB = commander.GetDataBlob<CommanderDB>();
                        var experience = commander.StarSysDateTime - commanderDB.CommissionedOn;
                        ImGui.Text(experience.ToYears().ToString("F0"));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("Commissioned on: " + commanderDB.CommissionedOn.ToShortDateString());
                        ImGui.TableNextColumn();
                        var rankTime = commander.StarSysDateTime - commanderDB.RankedOn;
                        ImGui.Text(rankTime.ToYears().ToString("F0"));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("Promoted on: " + commanderDB.RankedOn.ToShortDateString());
                    }

                    ImGui.EndTable();
                }
                Window.End();
            }
        }
    }
}