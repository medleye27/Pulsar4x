﻿using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using Vector3 = System.Numerics.Vector3;

namespace Pulsar4X.SDL2UI
{
    public enum TextAlign
    {
        Left,
        Center,
        Right
    }

    public class EntityNameSelector
    {
        public enum NameType
        {
            Owner,
            Default,
            Faction,
            Guids
        }
        private Entity[] _entities;
        private string[] _names;
        private int _index = 0;

        public EntityNameSelector(Entity[] entities, NameType nameType, int factionID)
        {
            _entities = entities;
            _names = new string[_entities.Length];
            if (nameType == NameType.Default)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    _names[i] = _entities[i].GetDefaultName();
                }
            }

            if (nameType == NameType.Owner)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    _names[i] = _entities[i].GetOwnersName();
                }
            }

            if (nameType == NameType.Faction)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    _names[i] = _entities[i].GetName(factionID);
                }
            }

            if (nameType == NameType.Guids)
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    _names[i] = _entities[i].Id.ToString();
                }
            }
        }

        public bool Combo(string label)
        {
            return ImGui.Combo(label, ref _index, _names, _names.Length);
        }

        public Entity GetSelectedEntity()
        {
            return _entities[_index];
        }

        public string GetSelectedName()
        {
            return _names[_index];
        }

        public bool IsItemSelected
        {
            get { return _index > -1; }
        }

    }

    public static class Helpers
    {
        public static void RenderImgUITextTable(KeyValuePair<string, TextAlign>[] headings, List<string[]> data)
        {
            List<int> maxLengthOfDataByColumn = new List<int>();
            for (int i = 0; i < headings.Length; i++)
                maxLengthOfDataByColumn.Add(headings[i].Key.Length);

            foreach (var row in data)
            {
                for (int i = 0; i < row.Length; i++)
                    maxLengthOfDataByColumn[i] = Math.Max(row[i].Length, maxLengthOfDataByColumn[i]);
            }

            // Draw Header Line
            string headerLine = "";
            for (int i = 0; i < headings.Length; i++)
            {
                headerLine += GetByAlignmentAndMaxLength(headings[i].Key, maxLengthOfDataByColumn[i], headings[i].Value);
            }

            if (headerLine.Replace(" ", "") != "")
            {
                ImGui.TextUnformatted(headerLine);
            }

            foreach (var row in data)
            {
                string rowLine = "";
                for (int i = 0; i < row.Length; i++)
                {
                    rowLine += GetByAlignmentAndMaxLength(row[i], maxLengthOfDataByColumn[i], headings[i].Value);
                }
                ImGui.TextUnformatted(rowLine);
            }
        }

        private static string GetByAlignmentAndMaxLength(string value, int maxDataLength, TextAlign alignment)
        {
            if (alignment == TextAlign.Left)
                return value.PadRight(maxDataLength + 1);

            if (alignment == TextAlign.Right)
                return value.PadLeft(maxDataLength + 1);

            // alignment == TextAlign.Center)
            if (maxDataLength % 2 == 1)
                maxDataLength++;

            int diffInLength = maxDataLength + 2 - value.Length;

            return value.PadLeft(value.Length + (diffInLength / 2)).PadRight(maxDataLength + 2);
        }


        public static Vector3 Color(byte r, byte g, byte b)
        {
            float rf = (1.0f / 255) * r;
            float gf = (1.0f / 255) * g;
            float bf = (1.0f / 255) * b;
            return new Vector3(rf, gf, bf);
        }

        public static byte Color(float color)
        {
            return (byte)(Math.Max(0, Math.Min(255, (int)Math.Floor(color * 256.0))));
        }

        public static double GetSingleDistanceSquared(float x, float y)
        {
            return (x - y) * (x - y);
        }

        public static double GetDistanceSquared(float x1, float y1, float x2, float y2)
        {
            return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
        }
    }

    public static class BorderListOptions
    {
        struct BorderListState
        {
            internal System.Numerics.Vector2 _labelSize;
            internal  float _xleft;
            internal  float _xcentr;
            internal  float _xright;

            internal  float _ytop;
            internal  float _yctr1;
            internal  float _yctr2;
            internal  float _ybot;

            internal  uint _colour;

            internal  float _lhHeight;
        }

        private static BorderListState[] _states = new BorderListState[8];
        private static float _dentFactor = 4;
        private static int _nestIndex = 0;

        private static int colomnCount = 1;
        /*
        private static Vector2 _labelSize = new Vector2();
        private static float _xleft;
        private static float _xcentr;
        private static float _xright;

        private static float _ytop;
        private static float _yctr1;
        private static float _yctr2;
        private static float _ybot;

        private static uint _colour;
        private static float _dentMulitpier = 3;
        private static float _lhHeight;
        */

        public static bool Begin(string id, string[] list, ref int selected, float width)
        {
            bool selectedChanged = false;
            ImGui.PushID(id);
            var state = new BorderListState();
            state._colour = ImGui.GetColorU32(ImGuiCol.Border);
            state._labelSize = new System.Numerics.Vector2( width, ImGui.GetTextLineHeight());
            colomnCount = ImGui.GetColumnsCount();
            ImGui.Columns(2, id, false);
            ImGui.SetColumnWidth(0, width);

            state._xleft = ImGui.GetCursorScreenPos().X;
            state._ytop = ImGui.GetCursorScreenPos().Y;
            state._xcentr = state._xleft + width;

            var vpad = ImGui.GetTextLineHeightWithSpacing() - ImGui.GetTextLineHeight();


            ImGui.Indent(_dentFactor);
            //display the list of items:
            for (int i = 0; i < list.Length; i++)
            {
                var pos = ImGui.GetCursorScreenPos();

                ImGui.Text(list[i]);
                if (ImGui.IsItemClicked())
                {
                    selected = i;
                    selectedChanged = true;
                }

                if(i == selected)
                {
                    state._yctr1 = pos.Y - vpad * 0.5f;
                    state._yctr2 = state._yctr1 + ImGui.GetTextLineHeightWithSpacing();
                }

            }


            state._ybot = ImGui.GetCursorScreenPos().Y;
            state._lhHeight = ImGui.GetContentRegionAvail().Y;
            //if nothing is selected we'll draw a line at the bottom instead of around one of the items:
            if(selected < 0)
            {
                state._yctr1 = state._ybot;
                state._yctr2 = state._ybot;
            }

            ImGui.NextColumn(); //set nextColomn so the imgui.items placed after this get put into the righthand side
            ImGui.Indent(_dentFactor * (_nestIndex + 1));
            _states[_nestIndex] = state;
            _nestIndex++;
            return selectedChanged;
        }
        /*
        public static void Begin(string id, ref int selected, string[] list)
        {
            Begin(id, list, ref selected, ImGui.GetColorU32(ImGuiCol.Border));
        }

        public static void Begin(string id, string[] list, ref int selected, ImGuiCol colorIdx)
        {
            Begin(id, list, ref selected, ImGui.GetColorU32(colorIdx));
        }
*/
        public static void End(System.Numerics.Vector2 sizeRight)
        {
            ImGui.Unindent(_dentFactor * (_nestIndex + 1));
            _nestIndex--;
            var state = _states[_nestIndex];
            var winpos = ImGui.GetCursorPos();

            var rgnSize = ImGui.GetContentRegionAvail();

            ImGui.NextColumn();
            ImGui.Columns(colomnCount);
            var scpos = ImGui.GetCursorScreenPos();
            ImGui.Unindent(_dentFactor);

            state._xright = state._xcentr + sizeRight.X + _dentFactor;

            float boty = Math.Max(state._ybot, state._ytop + sizeRight.Y); //is the list bigger, or the items drawn after it.

            ImDrawListPtr wdl = ImGui.GetWindowDrawList();


            System.Numerics.Vector2[] pts = new System.Numerics.Vector2[9];
            pts[0] = new System.Numerics.Vector2(state._xleft, state._yctr1);          //topleft of the selected item
            pts[1] = new System.Numerics.Vector2(state._xleft, state._yctr2);          //botomleft of the selected item
            pts[2] = new System.Numerics.Vector2(state._xcentr, state._yctr2);         //bottom rigth of selected item
            pts[3] = new System.Numerics.Vector2(state._xcentr, boty);           //bottom left of rh colomn
            pts[4] = new System.Numerics.Vector2(state._xright, boty);           //bottom Right
            pts[5] = new System.Numerics.Vector2(state._xright, state._ytop);          //top righht
            pts[6] = new System.Numerics.Vector2(state._xcentr, state._ytop);          //top mid
            pts[7] = new System.Numerics.Vector2(state._xcentr, state._yctr1);         //selected top right
            pts[8] = pts[0];                                    //selected top left

            var plflag = ImGuiNET.ImDrawFlags.None;
            wdl.AddPolyline(ref pts[0], pts.Length, state._colour, plflag, 1.0f);

            ImGui.PopID();

        }
    }




    public static class BorderGroup
    {
        private static System.Numerics.Vector2[] _startPos = new System.Numerics.Vector2[8];
        private static System.Numerics.Vector2[] _labelSize = new System.Numerics.Vector2[8];
        private static uint[] _colour = new uint[8];
        private static byte _nestIndex = 0;
        private static float _dentMulitpier = 3;
        private static System.Numerics.Vector2[] _size = new System.Numerics.Vector2[8];

        public static System.Numerics.Vector2 GetSize => _size[_nestIndex];

        public static void Begin(string label, uint colour)
        {
            ImGui.PushID(label);

            _colour[_nestIndex] = colour;
            _startPos[_nestIndex] = ImGui.GetCursorScreenPos();
            _startPos[_nestIndex].X -= _dentMulitpier;
            _startPos[_nestIndex].Y += ImGui.GetTextLineHeight() * 0.5f;
            ImGui.Text(label);
            _labelSize[_nestIndex] = ImGui.GetItemRectSize();
            _nestIndex++;
            ImGui.Indent(_dentMulitpier * _nestIndex);
        }

        public static void Begin(string label)
        {
            Begin(label, ImGui.GetColorU32(ImGuiCol.Border));
        }

        public static void Begin(string label, ImGuiCol colorIdx)
        {
            Begin(label, ImGui.GetColorU32(colorIdx));
        }

        public static void End()
        {
            End(ImGui.GetContentRegionAvail().X);
        }

        public static void End(float width)
        {
            ImGui.Unindent(_dentMulitpier * _nestIndex);
            _nestIndex--;
            var pos = ImGui.GetCursorScreenPos();

            _size[_nestIndex] = new System.Numerics.Vector2(width, pos.Y - _startPos[_nestIndex].Y);
            ImDrawListPtr wdl = ImGui.GetWindowDrawList();

            float by = _startPos[_nestIndex].Y + _size[_nestIndex].Y + _dentMulitpier -_dentMulitpier * _nestIndex;
            float rx = _startPos[_nestIndex].X + _size[_nestIndex].X - _dentMulitpier * _nestIndex;

            System.Numerics.Vector2[] pts = new System.Numerics.Vector2[6];
            pts[0] = new System.Numerics.Vector2(_startPos[_nestIndex].X + _dentMulitpier, _startPos[_nestIndex].Y);
            pts[1] = _startPos[_nestIndex]; //top left
            pts[2] = new System.Numerics.Vector2(_startPos[_nestIndex].X, by); //bottom left
            pts[3] = new System.Numerics.Vector2(rx, by); //bottom right
            pts[4] = new System.Numerics.Vector2(rx, _startPos[_nestIndex].Y); //top right
            pts[5] = new System.Numerics.Vector2(_startPos[_nestIndex].X + _labelSize[_nestIndex].X + _dentMulitpier, _startPos[_nestIndex].Y);
            var plflag = ImGuiNET.ImDrawFlags.None;
            wdl.AddPolyline(ref pts[0], pts.Length, _colour[_nestIndex], plflag, 1.0f);

            ImGui.PopID();

        }
    }

    public static class Switch
    {
        //private static int _intState = 0;
        public static bool Switch2State(string label, ref bool state, string leftState = "Off", string rightState = "On")
        {
            int intState = Convert.ToInt32(state);
            string strstate = leftState;
            if (state == true)
                strstate = rightState;
            var txtWid = Math.Max(ImGui.CalcTextSize(leftState).X, ImGui.CalcTextSize(rightState).X);
            ImGui.PushItemWidth(txtWid * 3);
            var cpos = ImGui.GetCursorPos();
            if(ImGui.SliderInt(label,ref intState, 0, 1, "" ))
            {
                state = Convert.ToBoolean(intState);
                return true;
            }
            System.Numerics.Vector2 recSize = ImGui.GetItemRectSize();
            float x = cpos.X  + 2 + (intState * (txtWid -4) * 2);
            float y = (float)(cpos.Y + recSize.Y * 0.5 - ImGui.GetTextLineHeight() * 0.5);
            ImGui.SetCursorPos(new System.Numerics.Vector2(x, y));
            ImGui.Text(strstate);
            ImGui.PopItemWidth();


            return false;
        }



    }


    public static class DistanceDisplay
    {
        public enum ValueType
        {
            Au,
            MKm,
            Km,
            m
        }

        public enum DisplayType
        {
            Raw,
            Global,
            Au,
            Mkm,
            Km,
            m
        }

        static DisplayType GlobalDisplayType = DisplayType.Km;
        static string GlobalFormat = "0.###";

        static string StringifyValue(double value, string format = "0.###")
        {
            return Stringify.Distance(value, format);
        }

        public static void Display(string Id, double value, ValueType inputType, ref DisplayType displayType, ref string displayFormat )
        {
            //ImGui.GetID(Id);

            ImGui.Text(StringifyValue(value, displayFormat));
            if(ImGui.BeginPopupContextItem(Id, ImGuiPopupFlags.MouseButtonRight))
            {
                if(ImGui.SmallButton("Set Display Type"))
                { }
                if(ImGui.SmallButton("Set Display Format"))
                { }

            }
        }

    }


    public static class LargeRangeSliderInt
    {
        public delegate int Step (int value);

        public static Step StepMethod = Step1;

        public static int Step1(int value)
        {
            return 1;
        }

        public static int StepLog2x(int value)
        {
            return Convert.ToInt32(Math.Log2(value)) ;
        }


        public static bool Display(string label, ref int value, int min, int max)
        {

            ImGui.PushID("largerangeslider");
            var step = StepMethod(value);
            bool changed = false;

            if (ImGui.Button("-100k"))
            {
                value = Math.Max(min, value - 100000);
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("-1k"))
            {
                value = Math.Max(min, value - 1000);
                changed = true;
            }ImGui.SameLine();
            if (ImGui.Button("-100"))
            {
                value = Math.Max(min, value - 100);
                changed = true;
            }ImGui.SameLine();
            if (ImGui.Button("-1"))
            {
                value = Math.Max(min, value - 1);
                changed = true;
            }ImGui.SameLine();

            if (ImGui.DragInt(label, ref value, step, min, max))
            {
                changed = true;
            }ImGui.SameLine();

            if (ImGui.Button("100k"))
            {
                value = Math.Min(max, value - 100000);
                changed = true;
            }ImGui.SameLine();
            if (ImGui.Button("1k"))
            {
                value = Math.Min(max, value - 1000);
                changed = true;
            }ImGui.SameLine();
            if (ImGui.Button("100"))
            {
                value = Math.Min(max, value - 100);
                changed = true;
            }ImGui.SameLine();
            if (ImGui.Button("1"))
            {
                value = Math.Min(max, value - 1);
                changed = true;
            }
            ImGui.PopID();

            return changed;

        }
    }

    public static class ImGuiExt
    {
        public static bool ButtonED(string label, bool IsEnabled)
        {

            if(!IsEnabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);

            bool clicked = ImGui.Button(label);

            if(!IsEnabled)
            {
                ImGui.PopStyleVar();
                clicked = false; //if we're not enabled, we return false.
            }
            return clicked;
        }

        public static bool SliderAngleED(string label, ref float v_rad, bool IsEnabled)
        {
            var rad = v_rad;
            if(!IsEnabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);

            bool clicked = ImGui.SliderAngle(label, ref v_rad);

            if(!IsEnabled)
            {
                ImGui.PopStyleVar();
                v_rad = rad;
                clicked = false; //if we're not enabled, we return false.
            }
            return clicked;
        }



        public static bool SliderDouble(string label, ref double value, double min, double max)
        {
            return SliderDouble(label, ref value, min, max, null, ImGuiSliderFlags.None);
        }

        public static bool SliderDouble(string label, ref double value, double min, double max, string? format, ImGuiSliderFlags flags)
        {
            if(string.IsNullOrEmpty(format))
            {
                format = "";
            }

            //double step = attribute.StepValue;
            //double fstep = step * 10;
            double val = value;
            IntPtr valPtr;
            IntPtr maxPtr;
            IntPtr minPtr;
            //IntPtr stepPtr;
            //IntPtr fstepPtr;

            unsafe
            {
                valPtr = new IntPtr(&val);
                maxPtr = new IntPtr(&max);
                minPtr = new IntPtr(&min);
                //stepPtr = new IntPtr(&step);
                //fstepPtr = new IntPtr(&fstep);
            }

            bool changed = false;
            if(ImGui.SliderScalar(label, ImGuiDataType.Double, valPtr, minPtr, maxPtr, format, flags))
            {
                value = val;
                changed = true;
            }
            return changed;
        }

        public static bool DragDouble(string label, ref double value, float v_speed, double min, double max, string format, ImGuiSliderFlags flags)
        {
            //double step = attribute.StepValue;
            //double fstep = step * 10;
            double val = value;
            IntPtr valPtr;
            IntPtr maxPtr;
            IntPtr minPtr;
            //IntPtr stepPtr;
            //IntPtr fstepPtr;

            unsafe
            {
                valPtr = new IntPtr(&val);
                maxPtr = new IntPtr(&max);
                minPtr = new IntPtr(&min);
                //stepPtr = new IntPtr(&step);
                //fstepPtr = new IntPtr(&fstep);
            }

            bool changed = false;
            if(ImGui.DragScalar(label, ImGuiDataType.Double, valPtr, v_speed, minPtr, maxPtr, format, flags))
            {
                value = val;
                changed = true;
            }
            return changed;
        }



    }

    public static class VectorWidget2d
    {
        public enum Style
        {
            Polar,
            Cartesian
        }

        private static Style _valueStyle = Style.Cartesian;
        private static Style _displayStyle = Style.Polar;

        /// <summary>
        ///
        /// </summary>
        /// <param name="values">x,y or r,θ</param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="valueStyle">if the values are in cartisian or polar coordinates</param>
        /// <returns></returns>
        public static bool Display(string label, ref Pulsar4X.Orbital.Vector2 values, int minVal = 0, int maxVal = int.MaxValue, Style valueStyle = Style.Cartesian)
        {
            ImGui.PushID(label);
            //BorderGroup.Begin(label);
            ImGui.Text(label);
            _valueStyle = valueStyle;
            bool changed = false;
            ImGui.SameLine();
            if(ImGui.SmallButton("Style"))
            {
                var nextStyle = (short)_displayStyle + 1;
                var max = Enum.GetValues(typeof(Style)).Length;
                if (nextStyle >= max)
                    nextStyle = 0;
                _displayStyle = (Style)nextStyle;
            }

            if (_displayStyle == Style.Cartesian)
            {
                changed = CartInt(ref values, minVal, maxVal);
            }
            else
            {
                changed = PolarInt(ref values, maxVal);
            }
            //BorderGroup.End();
            ImGui.PopID();
            return changed;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="values">x,y or r,θ</param>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="valueStyle">if the values are in cartisian or polar coordinates</param>
        /// <returns></returns>
        public static bool Display(string label, ref Pulsar4X.Orbital.Vector2 values, double minVal = 0, double maxVal = double.MaxValue, Style valueStyle = Style.Cartesian)
        {
            ImGui.PushID(label);
            //BorderGroup.Begin(label);
            ImGui.Text(label);
            _valueStyle = valueStyle;
            bool changed = false;
            ImGui.SameLine();
            if(ImGui.SmallButton("Style"))
            {
                var nextStyle = (short)_displayStyle + 1;
                var max = Enum.GetValues(typeof(Style)).Length;
                if (nextStyle >= max)
                    nextStyle = 0;
                _displayStyle = (Style)nextStyle;
            }

            if (_displayStyle == Style.Cartesian)
            {
                changed = CartDouble(ref values, minVal, maxVal);
            }
            else
            {
                changed = PolarDouble(ref values, maxVal);
            }
            //BorderGroup.End();
            ImGui.PopID();
            return changed;
        }

        static bool CartInt(ref Pulsar4X.Orbital.Vector2 values, int minVal, int maxVal)
        {
            bool changed = false;

            int x = 0;
            int y = 0;

            if (_valueStyle == Style.Cartesian)
            {
                x = (int)Math.Round(values.X);
                y = (int)Math.Round(values.Y);
            }
            else
            {
                x = (int)Math.Round(values.X * Math.Cos(values.Y));
                y = (int)Math.Round(values.X * Math.Sin(values.Y));
            }


            if (ImGui.SliderInt("X", ref x, minVal, maxVal))
                changed = true;
            if (ImGui.SliderInt("Y", ref y, minVal, maxVal))
                changed = true;


            if (changed)
            {
                if (_valueStyle == Style.Cartesian)
                {
                    values.X = x;
                    values.Y = y;
                }
                else
                {
                    values.X = Math.Sqrt((x * x) + (y * y));
                    values.Y = (float)Math.Atan2(y, x);
                }
            }
            return changed;
        }

        static bool CartDouble(ref Pulsar4X.Orbital.Vector2 values, double minVal, double maxVal)
        {
            bool changed = false;

            double x = 0;
            double y = 0;

            if (_valueStyle == Style.Cartesian)
            {
                x = Math.Round(values.X);
                y = Math.Round(values.Y);
            }
            else
            {
                x = Math.Round(values.X * Math.Cos(values.Y));
                y = Math.Round(values.X * Math.Sin(values.Y));
            }


            if (ImGuiExt.SliderDouble("X", ref x, minVal, maxVal, Stringify.Distance(x), ImGuiSliderFlags.AlwaysClamp))
                changed = true;
            if (ImGuiExt.SliderDouble("Y", ref y, minVal, maxVal, Stringify.Distance(x), ImGuiSliderFlags.AlwaysClamp))
                changed = true;


            if (changed)
            {
                if (_valueStyle == Style.Cartesian)
                {
                    values.X = x;
                    values.Y = y;
                }
                else
                {
                    values.X = Math.Sqrt((x * x) + (y * y));
                    values.Y = (float)Math.Atan2(y, x);
                }
            }
            return changed;
        }

        static bool PolarDouble(ref Pulsar4X.Orbital.Vector2 values, double maxVal)
        {

            bool changed = false;
            double r = 0;
            float theta = 0;
            if(_valueStyle == Style.Cartesian)
            {
                r = (int)Math.Round(values.Length());
                theta = (float)Math.Atan2(values.Y, values.X);
                Angle.NormaliseRadians(theta);
                while (theta < 0)
                    theta += (float)Math.PI * 2;
            }
            else
            {
                r =  (int)Math.Round(values.X);
                theta = (int)Math.Round(values.Y);
            }


            //if (ImGui.SliderInt("r", ref r, 0, maxVal, r.ToString(), ImGuiSliderFlags.ClampOnInput))

            //var step = (Math.Log(maxValue) - Math.Log(minValue))/(steps - 1);


            double maxMouseDelta = 1000;
            double mdelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).X;
            mdelta = Math.Min(mdelta, maxMouseDelta);
            double step = (Math.Log(maxVal) - Math.Log(1)) / maxMouseDelta;
            float speed = (float)(Math.Min(maxVal,Math.Exp(Math.Log(1) + mdelta * step)));
            //ImGui.Text("mdelta:" + mdelta);
            //ImGui.Text("step:" + step);
            //ImGui.Text("speed:" + speed);


            if(ImGuiExt.DragDouble("r", ref r, speed, 0, maxVal, Stringify.Distance(r), ImGuiSliderFlags.AlwaysClamp))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Radius");

            if (ImGui.SliderAngle("θ°", ref theta, 0f, 360f))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Angle");

            if (changed)
            {
                if (_valueStyle == Style.Cartesian)
                {
                    values.X = r * Math.Cos(theta);
                    values.Y = r * Math.Sin(theta);
                }
                else
                {
                    values.X = r;
                    values.Y = theta;
                }
            }
            return changed;
        }


        static bool PolarInt(ref Pulsar4X.Orbital.Vector2 values, int maxVal)
        {
            bool changed = false;
            int r = 0;
            float theta = 0;
            if(_valueStyle == Style.Cartesian)
            {
                r = (int)Math.Round(values.Length());
                theta = (float)Math.Atan2(values.Y, values.X);
                Angle.NormaliseRadians(theta);
                while (theta < 0)
                    theta += (float)Math.PI * 2;
            }
            else
            {
                r =  (int)Math.Round(values.X);
                theta = (int)Math.Round(values.Y);
            }


            //if (ImGui.SliderInt("r", ref r, 0, maxVal, r.ToString(), ImGuiSliderFlags.ClampOnInput))

            //var step = (Math.Log(maxValue) - Math.Log(minValue))/(steps - 1);


            double maxMouseDelta = 1000;
            double mdelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length();
            mdelta = Math.Min(mdelta, maxMouseDelta);
            double step = (Math.Log(maxVal) - Math.Log(1)) / maxMouseDelta;
            int speed = Convert.ToInt32(Math.Min(maxVal,Math.Exp(Math.Log(1) + mdelta * step)));
            //ImGui.Text("mdelta:" + mdelta);
            //ImGui.Text("step:" + step);
            //ImGui.Text("speed:" + speed);


            if(ImGui.DragInt("r", ref r, speed, 0, maxVal, r.ToString(), ImGuiSliderFlags.AlwaysClamp))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Radius");

            if (ImGui.SliderAngle("θ°", ref theta, 0f, 360f))
                changed = true;
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Angle");

            if (changed)
            {
                if (_valueStyle == Style.Cartesian)
                {
                    values.X = r * Math.Cos(theta);
                    values.Y = r * Math.Sin(theta);
                }
                else
                {
                    values.X = r;
                    values.Y = theta;
                }
            }
            return changed;
        }
    }
}


