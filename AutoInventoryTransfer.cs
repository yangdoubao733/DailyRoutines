global using static DailyRoutines.Infos.Widgets;
global using static OmenTools.Helpers.HelpersOm;
global using static DailyRoutines.Infos.Extensions;
global using static OmenTools.Infos.InfosOm;
global using static OmenTools.Helpers.ThrottlerHelper;
global using static DailyRoutines.Managers.Configuration;
global using static DailyRoutines.Managers.LanguageManagerExtensions;
global using static DailyRoutines.Helpers.NotifyHelper;
global using static OmenTools.Helpers.ContentsFinderHelper;
global using Dalamud.Interface.Utility.Raii;
global using OmenTools.Infos;
global using OmenTools.ImGuiOm;
global using OmenTools.Helpers;
global using OmenTools;
global using ImGuiNET;
global using ImPlotNET;
global using Lang = DailyRoutines.Managers.LanguageManager;
global using Dalamud.Game;

using DailyRoutines.Abstracts;
using DailyRoutines.Managers;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui.ContextMenu;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DailyRoutines.ModulesPublic;

public unsafe class AutoInventoryTransfer : DailyModuleBase
{
    public override ModuleInfo Info { get; } = new()
    {
        Title = "物品快速转移",
        Description = "按住左Shift键并右键点击物品时，自动转移物品\n" +
                      "· 陆行鸟鞍囊: 放入/取出物品\n" +
                      "· 雇员: 存入/取回物品",
        Category = ModuleCategories.UIOptimization,
    };

    // 陆行鸟鞍囊操作的菜单文本
    private readonly string[] saddlebagTexts = ["放入陆行鸟鞍囊", "从陆行鸟鞍囊取出"];
    
    // 雇员保管操作的菜单文本（含多语言支持）
    private readonly string[] entrustTexts = ["交给雇员保管", "从雇员处取回", "Entrust to Retainer", "リテイナーに預ける"];

    public override void Init()
    {
        TaskHelper ??= new() { TimeLimitMS = 2_000 };
        
        // 注册右键菜单打开事件
        DService.ContextMenu.OnMenuOpened += OnContextMenuOpened;
    }

    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        // 检查Shift键是否按下
        if (!IsConflictKeyPressed()) return;
        
        // 检查是否是物品菜单
        if (!IsValidInventoryMenu(args.AddonName)) return;

        // 处理陆行鸟鞍囊转移 (需要鞍囊窗口和物品栏窗口都打开)
        if (IsChocoboBagOpen() && IsInventoryOpen())
        {
            HandleSaddleBagTransfer();
            return;
        }

        // 处理雇员保管功能 (需要雇员物品窗口和物品栏窗口都打开)
        if (IsRetainerInventoryOpen() && IsInventoryOpen())
        {
            HandleRetainerEntrust();
            return;
        }
    }

    private void HandleSaddleBagTransfer()
    {
        TaskHelper.Abort();
        TaskHelper.Enqueue(() => 
        {
            // 确保上下文菜单已打开
            if (!IsAddonAndNodesReady(InfosOm.ContextMenu)) return false;

            // 尝试查找并点击陆行鸟鞍囊相关菜单项
            foreach (var text in saddlebagTexts)
            {
                if (ClickContextMenu(text))
                    return true;
            }
            return true;
        }, "点击陆行鸟鞍囊相关菜单项");
    }

    private void HandleRetainerEntrust()
    {
        TaskHelper.Abort();
        TaskHelper.Enqueue(() => 
        {
            // 确保上下文菜单已打开
            if (!IsAddonAndNodesReady(InfosOm.ContextMenu)) return false;

            // 尝试查找并点击"交给雇员保管"菜单项
            foreach (var text in entrustTexts)
            {
                if (ClickContextMenu(text))
                    return true;
            }
            
            return true;
        }, "点击交给雇员保管菜单项");
    }

    private bool IsValidInventoryMenu(string? addonName)
    {
        if (addonName == null) return false;
        
        // 检查是否是物品相关的菜单
        return addonName.StartsWith("Inventory") || 
               addonName == "InventoryGrid" ||
               addonName == "InventoryLarge" ||
               addonName == "SaddleBagGrid" ||
               addonName == "SaddleBag" ||
               addonName == "RetainerGrid" ||
               addonName == "RetainerInventoryLarge";
    }

    private bool IsChocoboBagOpen()
    {
        // 检查是否有陆行鸟鞍囊窗口打开
        return TryGetAddonByName<AtkUnitBase>("SaddleBag", out _) || 
               TryGetAddonByName<AtkUnitBase>("SaddleBagGrid", out _);
    }
    
    private bool IsInventoryOpen()
    {
        // 检查是否有玩家物品栏窗口打开
        return TryGetAddonByName<AtkUnitBase>("Inventory", out _) || 
               TryGetAddonByName<AtkUnitBase>("InventoryGrid", out _) ||
               TryGetAddonByName<AtkUnitBase>("InventoryLarge", out _);
    }

    private bool IsRetainerInventoryOpen()
    {
        // 检查是否有雇员物品管理窗口打开
        return TryGetAddonByName<AtkUnitBase>("RetainerGrid", out _) || 
               TryGetAddonByName<AtkUnitBase>("RetainerInventoryLarge", out _);
    }

    public override void ConfigUI()
    {
        ImGui.TextColored(LightSkyBlue, "使用说明:");
        
        using (ImRaii.PushIndent())
        {
            ImGui.TextWrapped("按住 Shift 键并右键点击物品时，根据当前打开的窗口自动执行以下操作：");
            
            ImGui.Spacing();
            ImGui.BulletText("当陆行鸟鞍囊窗口打开时：自动放入或取出物品");
            ImGui.BulletText("当雇员物品窗口打开时：自动存入或取回物品");
        }
    }

    public override void Uninit()
    {
        // 取消注册右键菜单事件
        DService.ContextMenu.OnMenuOpened -= OnContextMenuOpened;
        
        TaskHelper.Abort();
        base.Uninit();
    }
}
