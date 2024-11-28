using ImGuiNET;
using Pulsar4X.Modding;

namespace Pulsar4X.SDL2UI.ModFileEditing;

public class ModFileEditor : PulsarGuiWindow
{
    private ModInfoUI _modInfoUI;
    private TechBlueprintUI _techBlueprintUI;
    private TechCatBlueprintUI _techCatBlueprintUI;
    private ComponentBluprintUI _componentBluprintUI;
    private CargoTypeBlueprintUI _cargoTypeBlueprintUI;
    private AttributeBlueprintUI _attributeBlueprintUI;
    private ArmorBlueprintUI _armorBlueprintUI;
    private ProcessedMateralsUI _processedMateralsUI;
    private MineralBlueprintUI _mineralsBlueprintUI;
    
    
    private ModFileEditor()
    {

    }
    internal static ModFileEditor GetInstance()
    {
        ModFileEditor instance;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(ModFileEditor)))
        {
            instance = new ModFileEditor();
            ModLoader modLoader = new ModLoader();
            ModDataStore modDataStore = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", modDataStore);
            instance.Refresh(modDataStore);
        }
        else
        {
            instance = (ModFileEditor)_uiState.LoadedWindows[typeof(ModFileEditor)];
        }
        return instance;
    }

    public void Refresh(ModDataStore modDataStore)
    {
        _modInfoUI = new ModInfoUI(modDataStore);
        _techCatBlueprintUI = new TechCatBlueprintUI(modDataStore);
        _techBlueprintUI = new TechBlueprintUI(modDataStore);
        _componentBluprintUI = new ComponentBluprintUI(modDataStore);
        _cargoTypeBlueprintUI = new CargoTypeBlueprintUI(modDataStore);

        _armorBlueprintUI = new ArmorBlueprintUI(modDataStore);
        _processedMateralsUI = new ProcessedMateralsUI(modDataStore);
        _mineralsBlueprintUI = new MineralBlueprintUI(modDataStore);
    }

    
    internal override void Display()
    {
        
        if (IsActive)
        {
            if (ImGui.Begin("Editor", ref IsActive))
            {
                _modInfoUI.Display("Mod Info");
                ImGui.NewLine();
                _techCatBlueprintUI.Display("Tech Categorys");
                ImGui.NewLine();
                _techBlueprintUI.Display("Techs");
                ImGui.NewLine();
                _componentBluprintUI.Display("Components");
                ImGui.NewLine();
                _cargoTypeBlueprintUI.Display("Cargo Types");
                ImGui.NewLine();
                _armorBlueprintUI.Display("Armor");
                ImGui.NewLine();
                _processedMateralsUI.Display("Processed Materials");
                ImGui.NewLine();
                _mineralsBlueprintUI.Display("Minerals");
                ImGui.NewLine();
            }

            ImGui.End();
        }
    }
}