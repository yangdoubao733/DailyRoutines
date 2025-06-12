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

public unsafe class AutoConfirmMarketPurchase : DailyModuleBase
{
    public override ModuleInfo Info { get; } = new()
    {
        Title = "市场购买确认快捷键",
        Description = "按住Shift键时，自动点击市场布告板购买确认窗口的确定按钮",
        Category = ModuleCategories.UIOptimization,
    };

    public override void Init()
    {
        TaskHelper ??= new() { TimeLimitMS = 2_000 };
        
        // 注册市场购买确认窗口的监听
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ShopExchangeCurrencyDialog", OnShopExchangeDialogOpen);
        DService.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ShopExchangeCurrencyDialog", OnShopExchangeDialogClose);
    }

    private void OnShopExchangeDialogOpen(AddonEvent type, AddonArgs? args)
    {
        // 如果不是来自市场布告板的购买确认，则不处理
        if (!IsMarketBoardPurchase()) return;
        
        // 检查是否按下了Shift键
        if (!DService.KeyState[VirtualKey.SHIFT]) return;
        
        // 延迟一小段时间再执行确认操作，确保窗口已完全加载
        TaskHelper.Abort();
        TaskHelper.DelayNext(100, "等待确认窗口加载完成");
        TaskHelper.Enqueue(ClickConfirmButton, "点击市场购买确认按钮");
    }

    private void OnShopExchangeDialogClose(AddonEvent type, AddonArgs? args)
    {
        // 窗口关闭时中断任务
        TaskHelper.Abort();
    }

    private bool? ClickConfirmButton()
    {
        // 确保窗口仍然打开且准备就绪
        if (!IsAddonAndNodesReady(ShopExchangeCurrencyDialog)) return false;
        
        // 确认按钮通常是ID为17的按钮
        var buttonNode = ShopExchangeCurrencyDialog->GetButtonNodeById("确定");
        if (buttonNode == null) return false;
        
        // 点击确认按钮
        Callback(ShopExchangeCurrencyDialog, true, 0, buttonNode);
        return true;
    }

    private bool IsMarketBoardPurchase()
    {
        // 检查是否在市场布告板界面
        return TryGetAddonByName<AtkUnitBase>("ItemSearchResult", out _) || 
               TryGetAddonByName<AtkUnitBase>("ItemSearch", out _);
    }

    public override void Uninit()
    {
        DService.AddonLifecycle.UnregisterListener(OnShopExchangeDialogOpen);
        DService.AddonLifecycle.UnregisterListener(OnShopExchangeDialogClose);
        
        TaskHelper.Abort();
        base.Uninit();
    }
}
