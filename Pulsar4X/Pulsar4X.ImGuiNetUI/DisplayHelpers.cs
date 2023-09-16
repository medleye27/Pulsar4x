using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class DisplayHelpers
    {
        public static void Header(string text, string tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(text);
            if(!string.IsNullOrEmpty(tooltip))
            {
                ImGui.SameLine();
                ImGui.Text("[?]");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
            }
            ImGui.PopStyleColor();
            ImGui.Separator();
        }

        public static void PrintRow(string one, string two, string tooltipOne = null, string tooltipTwo = null, bool separator = true)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(one);
            ImGui.PopStyleColor();
            if(tooltipOne != null)
            {
                if(ImGui.IsItemHovered()) ImGui.SetTooltip(tooltipOne);
            }
            ImGui.NextColumn();

            ImGui.Text(two);
            if(tooltipTwo != null)
            {
                if(ImGui.IsItemHovered()) ImGui.SetTooltip(tooltipTwo);
            }
            ImGui.NextColumn();

            if(separator)
                ImGui.Separator();
        }

        public static void ShipTooltip(Entity ship)
        {
            if(!ship.TryGetDatablob<ShipInfoDB>(out var shipInfo))
                return;

            if(!ship.TryGetDatablob<OrderableDB>(out var orderableDB))
                return;

            if(ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(shipInfo.Design.Name);
                ImGui.Separator();
                if(orderableDB.ActionList.Count > 0)
                {
                    ImGui.Text("Orders:");
                    foreach(var action in orderableDB.ActionList)
                    {
                        ImGui.Text(action.Name);
                        ImGui.SameLine();
                        if(action.IsRunning)
                            ImGui.Text("(running)");
                        else
                            ImGui.Text("not running");
                    }
                }
                else
                {
                    ImGui.Text("No orders");
                }
                ImGui.EndTooltip();
            }
        }

        public static void DescriptiveTooltip(string name, string type, string description, bool hideTypeIfSameAsName = false)
        {
            if(ImGui.IsItemHovered())
            {
                ImGui.SetNextWindowSize(Styles.ToolTipsize);
                ImGui.BeginTooltip();
                ImGui.Text(name);
                if(type.IsNotNullOrEmpty() && (!hideTypeIfSameAsName || (hideTypeIfSameAsName && !type.Equals(name))))
                {
                    var size = ImGui.GetContentRegionAvail();
                    var textSize = ImGui.CalcTextSize(type);
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(size.X - textSize.X);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                    ImGui.Text(type);
                    ImGui.PopStyleColor();
                }
                if(description.IsNotNullOrEmpty())
                {
                    ImGui.Separator();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextWrapped(description);
                    ImGui.PopStyleColor();
                }
                ImGui.EndTooltip();
            }
        }
    }
}