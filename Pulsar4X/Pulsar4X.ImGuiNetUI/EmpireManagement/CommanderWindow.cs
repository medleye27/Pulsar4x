using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ImGuiNetUI;

namespace Pulsar4X.SDL2UI
{
    public class CommanderWindow : PulsarGuiWindow
    {
        private Entity _faction;
        private FactionInfoDB _factionInfoDB;
        private Dictionary<Guid, Entity> _commanders;
        private CommanderWindow()
        {
            _faction = _uiState.Faction;
            _factionInfoDB = _faction.GetDataBlob<FactionInfoDB>();
            _commanders = new Dictionary<Guid, Entity>();
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

            if(ImGui.Begin("Commanders", ref IsActive, _flags))
            {
                if(ImGui.BeginTable("CommanderTable", 2, Styles.TableFlags))
                {
                    ImGui.TableSetupColumn("Commander");
                    ImGui.TableSetupColumn("Yrs of Service");
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
                            commander = _uiState.Game.GlobalManager.GetGlobalEntityByGuid(commanderID);
                            _commanders.Add(commanderID, commander);
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text(commander.GetName(_faction.Guid));
                        ImGui.TableNextColumn();
                        var commanderDB = commander.GetDataBlob<CommanderDB>();
                        var experience = commander.StarSysDateTime - commanderDB.CommissionedOn;
                        ImGui.Text(experience.ToYears().ToString("F0"));
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}