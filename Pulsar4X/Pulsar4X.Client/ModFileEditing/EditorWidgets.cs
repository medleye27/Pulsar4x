using System.Collections.Generic;
using ImGuiNET;
using ImGuiSDL2CS;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public static class TextEditWidget
{
    private static uint _buffSize = 128;
    private static byte[] _strInputBuffer = new byte[128];
    private static string? _editingID;
    
    public static uint BufferSize
    {
        get { return _buffSize ;}
        set
        {
            _buffSize = value;
            _strInputBuffer = new byte[value];
        }
    }
    
    public static bool Display(string label, ref string text)
    {
        bool hasChanged = false;
        if(label != _editingID)
        {
            ImGui.Text(text);
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                _strInputBuffer = ImGuiSDL2CSHelper.BytesFromString(text);

            }
        }
        else
        {
            if (ImGui.InputText(label, _strInputBuffer, _buffSize, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                text = ImGuiSDL2CSHelper.StringFromBytes(_strInputBuffer);
                _editingID = null;
                hasChanged = true;
            }
        }

        return hasChanged;
    }
}
public static class IntEditWidget
{
    private static string? _editingID;
    
    public static bool Display(string label, ref int num)
    {
        bool hasChanged = false;
        if(label != _editingID)
        {
            ImGui.Text(num.ToString());
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
            }
        }
        else
        {
            if (ImGui.InputInt(label, ref num, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _editingID = null;
                hasChanged = true;
            }
        }

        return hasChanged;
    }
}

public static class DictEditWidget
{
    private static string? _editingID;
    private static int _editInt;
    private static string _editStr;

    private static uint _buffSize = 128;
    private static byte[] _strInputBuffer = new byte[128];
    private static int _techIndex = 0;
    
    public static bool Display(string label, ref Dictionary<int, List<string>> dict, string[] techs)
    {
        ImGui.BeginChild("##dic");
        ImGui.Columns(2);
        bool isChanged = false;
        int addnum = -1;
        foreach (var kvp in dict)
        {
            _editInt = kvp.Key;
            if (IntEditWidget.Display(label + _editInt, ref _editInt))
            {
                isChanged = true;
                if(!dict.ContainsKey(_editInt))
                    dict.Add(_editInt,kvp.Value);
            }
            ImGui.NextColumn();
            //values list
            foreach (var item in kvp.Value)
            {
                _editStr = item;
                if(TextEditWidget.Display(label+_editInt+item, ref _editStr))
                {
                        
                }
            }
            if(_editingID != label+"addValue")
            {
                if (ImGui.Button("+"))
                {
                    _editingID = label+"addValue";
                }
            }
            else
            {
                if (SelectFromListWiget.Display(label+"addValue", techs, ref _techIndex))
                {
                    dict[kvp.Key].Add(techs[_techIndex]);
                }
            }
            ImGui.NextColumn();
            if(_editingID != label+"addKey")
            {
                if (ImGui.Button("+"))
                {
                    _editingID = label+"addKey";
                }
            }
            else
            {
                addnum = dict.Keys.Count;
                while (dict.ContainsKey(addnum))
                    addnum++;
                _editingID = null;
            }
        }
        if(addnum > -1) //do this here so we don't add in the middle of foreach
            dict.Add(addnum, new List<string>());
        
        ImGui.EndChild();
        return isChanged;
    }
    
    public static bool Display(string label, ref Dictionary<string, string> dict)
    {
        ImGui.BeginChild("##dic");
        ImGui.Columns(2);
        bool isChanged = false;
        foreach (var kvp in dict)
        {
            _editStr = kvp.Key;
            if (TextEditWidget.Display(label + _editInt, ref _editStr))
            {
                isChanged = true;
                if(!dict.ContainsKey(_editStr))
                    dict.Add(_editStr,kvp.Value);
            }
            ImGui.NextColumn();
            //values list

            _editStr = kvp.Value;
            if(TextEditWidget.Display(label+kvp.Value, ref _editStr))
            {
                dict[kvp.Key] = _editStr;
            }
            
            ImGui.NextColumn();
        }
        
        ImGui.EndChild();

        return isChanged;
    }
}

public static class SelectFromListWiget
{
    private static string? _editingID;
    private static int _currentItem;
    private static string[] _items;
    private static int _itemCount;
    
    public static bool Display(string label, string[] selectFrom, ref int selected)
    {
        bool hasChanged = false;
        string displayText = "";
        if(selected > -1)   
            displayText = selectFrom[selected];
        if (label != _editingID)
        {
            ImGui.Text(displayText);
            if(ImGui.IsItemClicked())
            {
                _editingID = label;
                _items = selectFrom;
                _itemCount = _items.Length;
            }
        }
        else
        {
            ImGui.Text(displayText);
            ImGui.SameLine();
            if (ImGui.ListBox(label, ref _currentItem, _items, _itemCount))
            {
                selected = _currentItem;
                _editingID = null;
                hasChanged = true;
            }
        }
        return hasChanged;
    }
}