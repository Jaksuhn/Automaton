using Automaton.FeaturesSetup;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.Hooking;
using Dalamud.Memory;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Automaton.Features.UI
{
    public unsafe class Lifu : Feature
    {
        public override string Name => "Lifu";

        public override string Description => "";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public Configs Config { get; private set; }
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Auto Target", "", 1)]
            public bool AutoTarget = true;
            [FeatureConfigOption("Leve Quest ID", "", 2, IntMin = 0, IntMax = 100, EditorSize = 300)]
            public int LeveQuestId = 1635;
            [FeatureConfigOption("Target Delay", "", 3, IntMin = 0, IntMax = 100, EditorSize = 300)]
            public int TargetDelay = 1500;
            [FeatureConfigOption("Leve NPC 1")]
            public string LeveNpc1 = "格里格";
            [FeatureConfigOption("Leve NPC 2")]
            public string LeveNpc2 = "阿尔德伊恩";
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox("auto target", ref Config.AutoTarget)) hasChanged = true;
            ImGui.PushItemWidth(300);
            if (ImGui.SliderInt("leve id", ref Config.LeveQuestId, 0, 10000)) hasChanged = true;
            ImGui.PushItemWidth(300);
            if (ImGui.SliderInt("delay", ref Config.TargetDelay, 0, 3000)) hasChanged = true;

            if (ImGui.Button("toggle ui")) { settingsVisible ^= true; }
        };

        public override void Enable()
        {
            base.Enable();
            accessGameObject = Marshal.GetDelegateForFunctionPointer<AccessGameObjDelegate>(Svc.SigScanner.ScanText("E9 ?? ?? ?? ?? 48 8B 01 FF 50 08"));

            takenQeustParam1 = Svc.SigScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? 8B 44 24 70");
            InvManager = (IntPtr)InventoryManager.Instance();

            nextClick = DateTime.Now;
            nextTarget = DateTime.Now;

            takenQeustHook ??= Svc.Hook.HookFromAddress<TakenQeustHook>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 D8 EB ?? 48 8B 01"), TakenQeustDetour);
            takenQeustHook.Enable();
            requestHook ??= Svc.Hook.HookFromAddress<RequestHook>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CE E8 ?? ?? ?? ?? 48 8B 5C 24 40 4C 8B 74 24 48"), RequestDetour);
            requestHook.Enable();
            leveHook ??= Svc.Hook.HookFromAddress<LeveHook>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D BB ?? ?? ?? ?? 33 D2 8D 4E 10"), LeveDetour);
            leveHook.Enable();

            RaptureAtkUnitManager = AtkStage.GetSingleton()->RaptureAtkUnitManager;
            Svc.Framework.Update += Update;
            levesheet = Svc.Data.GetExcelSheet<Leve>();
            itemsheet = Svc.Data.GetExcelSheet<Item>();
            leveList = new List<Leve>();

            var craftLeves = Svc.Data.GetExcelSheet<CraftLeve>();
            var gatheringLeves = Svc.Data.GetExcelSheet<GatheringLeve>();

            foreach (var leve in craftLeves)
            {
                if (leve.Leve?.Value != null && leve.Leve?.Value.DataId != 0)
                {
                    leveList.Add(leve.Leve?.Value);
                }
            }

            foreach (var leve in gatheringLeves)
            {
                var l = levesheet.Where(i => i.DataId == leve.RowId).FirstOrDefault();
                if (l != null)
                {
                    leveList.Add(l);
                }
            }

            SetLeve();
        }

        public override void Disable()
        {
            base.Disable();
            takenQeustHook.Disable();
            requestHook.Disable();
            leveHook.Disable();
            Svc.Framework.Update -= Update;
        }

        private AccessGameObjDelegate? accessGameObject;
        private delegate void AccessGameObjDelegate(IntPtr g_ControlSystem_TargetSystem, IntPtr targte, char p3);

        private Hook<TakenQeustHook> takenQeustHook;
        private delegate IntPtr TakenQeustHook(long a1, long questId);
        private IntPtr takenQeustParam1;

        private Hook<RequestHook> requestHook;
        private delegate IntPtr RequestHook(long a, InventoryItem* b, int c, Int16 d, byte e);
        public IntPtr InvManager;
        public InventoryItem* TargetInvSlot = (InventoryItem*)IntPtr.Zero;

        private delegate IntPtr LeveHook(IntPtr a);
        private Hook<LeveHook> leveHook;
        private static RaptureAtkUnitManager* RaptureAtkUnitManager;
        private ExcelSheet<Leve> levesheet;
        private List<Leve> leveList;
        private ExcelSheet<Item> itemsheet;
        private int leveQuestId;
        private string leveQuestName;
        private string targetItemName = "N/A";
        private int leveItemId;
        private const int LeveItemMagic = 2005;
        private string leveNpc1;
        private string leveNpc2;

        private DateTime nextClick;
        private DateTime nextTarget;

        private IntPtr RequestDetour(long a, InventoryItem* b, int c, short d, byte e) => requestHook.Original(a, b, c, d, e);
        private IntPtr TakenQeustDetour(long a1, long a2) => takenQeustHook.Original(a1, a2);
        private IntPtr LeveDetour(IntPtr a) => leveHook.Original(a);

        private void SetLeve()
        {
            leveQuestId = Config.LeveQuestId;
            leveQuestName = Svc.Data.GetExcelSheet<Leve>().GetRow((uint)leveQuestId).Name;
            var DataId = Svc.Data.GetExcelSheet<Leve>().GetRow((uint)leveQuestId).DataId;
            leveItemId = Svc.Data.GetExcelSheet<CraftLeve>().GetRow((uint)DataId).UnkData3[0].Item;
            targetItemName = Svc.Data.GetExcelSheet<Item>().GetRow((uint)leveItemId).Name;
            leveNpc1 = Config.LeveNpc1;
            leveNpc2 = Config.LeveNpc2;
        }

        private readonly Random rd = new();

        private bool await = false;

        private void Update(IFramework framework)
        {
            var isMainMenu = !Svc.Condition.Any();
            if (isMainMenu) return;

            if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent] || Svc.Condition[ConditionFlag.OccupiedInEvent])
            {
                if (Enabled)
                {
                    var now = DateTime.Now;
                    if (nextClick > now)
                    {
                        return;
                    }

                    nextClick = DateTime.Now.AddMilliseconds(Math.Min(100, rd.Next(2) * 100));
                    TickTalk();
                    SelectString("有什么事？", 3);
                    SelectIconString(leveQuestName);
                    SubmitQuestItem(LeveItemMagic);
                    SelectYes("确定要交易优质道具吗？");
                    TickQuestComplete();

                    nextTarget = DateTime.Now.AddMilliseconds(Config.TargetDelay);
                    await = false;
                }
            }
            else
            {
                if (Enabled && Config.AutoTarget)
                {
                    if ((IntPtr)TargetInvSlot != IntPtr.Zero && TargetInvSlot->ItemID == 0)
                    {
                        FindItem(); // We're trying to find the item again to hand over un-stackable shit
                        if ((IntPtr)TargetInvSlot != IntPtr.Zero && TargetInvSlot->ItemID == 0)
                        {
                            Enabled = false;
                            TargetInvSlot = (InventoryItem*)IntPtr.Zero;
                            Svc.Log.Error("背包内没有理符要求的物品!");
                            return;
                        }
                    }

                    if (!await && DateTime.Now > nextTarget)
                    {
                        if (!IsLeveExists((ushort)leveQuestId) && QuestManager.Instance()->NumLeveAllowances <= 0)
                        {
                            Enabled = false;
                            Svc.Log.Error("理符限额不足!");
                            return;
                        }

                        TargetByName(!IsLeveExists((ushort)leveQuestId) ? Config.LeveNpc1 : Config.LeveNpc2);
                        await = true;
                    }
                }
            }
        }

        public void Toggle()
        {
            if (!Enabled)
            {
                TargetInvSlot = (InventoryItem*)IntPtr.Zero;
                FindItem();
                if ((IntPtr)TargetInvSlot == IntPtr.Zero)
                {
                    Svc.Log.Error("背包内没有理符要求的物品!");
                    Svc.Log.Error("如果是武器, 请放到背包, 不要放在兵装库!");
                    return;
                }
            }

            Enabled = !Enabled;
            Svc.Toasts.ShowQuest("理符辅助 " + (Enabled ? "开启" : "关闭"),
            new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
        }

        private bool settingsVisible = false;

        private void ToggleUI()
        {
            settingsVisible = !settingsVisible;
        }

        private string filter = "";

        public override void Draw()
        {
            if (!settingsVisible)
            {
                return;
            }

            if (ImGui.Begin("理符设置", ref this.settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (ImGui.Button($"{(Enabled ? "禁用" : "启用")}理符助手"))
                {
                    Toggle();
                }

                ImGui.SameLine();
                ImGui.Text($"[需要背包内拥有 {targetItemName}]");

                if (ImGui.BeginCombo("目标理符", leveQuestName, ImGuiComboFlags.None))
                {
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##LeveFilter", "过滤...", ref filter, 255);

                    foreach (var leve in leveList)
                    {
                        if (!filter.IsNullOrEmpty() && !leve.Name.ToString().Contains(filter))
                        {
                            continue;
                        }

                        if (ImGui.Selectable(leve.Name))
                        {
                            Config.LeveQuestId = (int)leve.RowId;
                            SetLeve();
                        }
                    }

                    ImGui.EndCombo();
                }

                var _autoTarget = Config.AutoTarget;
                if (ImGui.Checkbox("自动选择NPC", ref _autoTarget))
                {
                    Config.AutoTarget = _autoTarget;
                }

                var _targetDelay = Config.TargetDelay;
                if (ImGui.InputInt("自动选择NPC延时(毫秒)", ref _targetDelay))
                {
                    Config.TargetDelay = Math.Max(0, _targetDelay);
                }

                var _npc1 = Config.LeveNpc1;
                if (ImGui.InputText("接任务NPC", ref _npc1, 16))
                {
                    Config.LeveNpc1 = _npc1;
                    SetLeve();
                }
                ImGui.SameLine();
                if (ImGui.Button("选中"))
                {
                    TargetByName(Config.LeveNpc1);
                }

                var _npc2 = Config.LeveNpc2;
                if (ImGui.InputText("交任务NPC", ref _npc2, 16))
                {
                    Config.LeveNpc2 = _npc2;
                    SetLeve();
                }
                ImGui.SameLine();
                if (ImGui.Button("选中2"))
                {
                    TargetByName(Config.LeveNpc2);
                }

                ImGui.Text("如果不想用插件的自动选择NPC，请使用SND之类的插件执行指令进行手动选择。");
            }
        }

        private void TickTalk()
        {
            var addon = Svc.GameGui.GetAddonByName("Talk", 1);
            if (addon == IntPtr.Zero) return;
            var talkAddon = (AtkUnitBase*)addon;
            if (!talkAddon->IsVisible) return;

            var questAddon = (AtkUnitBase*)addon;
            var textComponent = (AtkComponentNode*)questAddon->UldManager.NodeList[20];
            var a = (AtkTextNode*)textComponent;

            if (leveNpc1 == Marshal.PtrToStringUTF8((IntPtr)a->NodeText.StringPtr) && !IsLeveExists((ushort)leveQuestId))
            {
                var b = Marshal.ReadInt64(takenQeustParam1);
                if (b > 0) takenQeustHook.Original(b, leveQuestId);
            }
            else
            {   //跳对话
                ClickTalk.Using(addon).Click();
                //clickManager.SendClick(addon, ClickManager.EventType.MOUSE_CLICK, 0, ((AddonTalk*)talkAddon)->AtkEventListenerUnk.AtkStage);
            }
        }

        private bool IsLeveExists(ushort leveId)
        {
            return QuestManager.Instance()->GetLeveQuestById(leveId) != null;
        }

        private bool takenLeve(string text)
        {
            var dataHolder = ((UIModule*)Svc.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
            var dataHolderContent = dataHolder.StringArrays[22];
            var size = dataHolderContent->AtkArrayData.Size;
            var array = dataHolderContent->StringArray;
            for (var i = 0; i < size; i++)
            {
                if (array[i] == null) continue;
                var seString = MemoryHelper.ReadSeString((FFXIVClientStructs.FFXIV.Client.System.String.Utf8String*)array[i]).TextValue;
                if (seString.Contains(text)) return true;
            }
            return false;
        }

        private void TickQuestComplete()
        {
            var addon = Svc.GameGui.GetAddonByName("JournalResult", 1);
            if (addon == IntPtr.Zero) return;
            var questAddon = (AtkUnitBase*)addon;
            if (questAddon->UldManager.NodeListCount <= 4) return;
            var buttonNode = (AtkComponentNode*)questAddon->UldManager.NodeList[4];
            if (buttonNode->Component->UldManager.NodeListCount <= 2) return;
            var textComponent = (AtkTextNode*)buttonNode->Component->UldManager.NodeList[2];
            if ("完成" != Marshal.PtrToStringUTF8((IntPtr)textComponent->NodeText.StringPtr)) return;
            if (!((AddonJournalResult*)addon)->CompleteButton->IsEnabled) return;
            ClickJournalResult.Using(addon).Complete();
            //clickManager.SendClickThrottled(addon, EventType.CHANGE, 1, ((AddonJournalResult*)addon)->CompleteButton->AtkComponentBase.OwnerNode);
        }

        private void SubmitQuestItem(int itemSId)
        {
            var addon = Svc.GameGui.GetAddonByName("Request", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible) return;
            if (selectStrAddon->UldManager.NodeListCount <= 3) return;
            var HighlighIcon = (AtkComponentNode*)selectStrAddon->UldManager.NodeList[16];
            var Ready = !HighlighIcon->AtkResNode.IsVisible;
            var focusedAddon = GetFocusedAddon();
            var addonName = focusedAddon != null ? Marshal.PtrToStringAnsi((IntPtr)focusedAddon->Name) : string.Empty;

            if (!Ready == true && InvManager != 0)
            {
                //查找物品内存地址并提交
                if ((IntPtr)TargetInvSlot == IntPtr.Zero || TargetInvSlot->ItemID == 0)
                {
                    FindItem(); // Just incase
                }
                else
                {
                    requestHook.Original(InvManager, TargetInvSlot, itemSId, 0, 1);
                }
            }

            if (Ready)
            {
                var questAddon = (AtkUnitBase*)addon;
                var buttonNode = (AtkComponentNode*)questAddon->UldManager.NodeList[4];
                if (buttonNode->Component->UldManager.NodeListCount <= 2) return;
                var textComponent = (AtkTextNode*)buttonNode->Component->UldManager.NodeList[2];
                var abc = Marshal.PtrToStringUTF8((IntPtr)textComponent->NodeText.StringPtr);

                if ("递交" != Marshal.PtrToStringUTF8((IntPtr)textComponent->NodeText.StringPtr)) return;
                var eventListener = (AtkEventListener*)addon;
                var receiveEventAddress = new IntPtr(eventListener->vfunc[2]);
                if (addonName == "Request")
                {
                    //点击提交
                    ClickRequest.Using(addon).HandOver();
                    //clickManager.SendClickThrottled(addon, EventType.CHANGE, 0, buttonNode);
                }
                //else
                //{//点击前先焦点
                //    clickManager.SendClickThrottled(addon, EventType.FOCUS_MAX, 2, buttonNode);
                //}
            }
        }

        public static AtkUnitBase* GetFocusedAddon()
        {
            var units = RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            var count = units.Count;
            return (AtkUnitBase*)(count == 0 ? null : (&units.Entries)[count - 1]);
        }

        private void FindItem()
        {
            for (var i = 0; i < 4; ++i) // Inventory1-4
            {
                var container = InventoryManager.Instance()->GetInventoryContainer((InventoryType)i);
                for (var j = 0; j < container->Size; ++j)
                {
                    var item = container->GetInventorySlot(j);
                    if (item is not null && item->ItemID == leveItemId) // 理符所需的物品ID
                    {
                        TargetInvSlot = item;
                        break;
                    }
                }
            }
        }

        private void SelectString(string title, int index)
        {
            var addon = Svc.GameGui.GetAddonByName("SelectString", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible) return;
            if (selectStrAddon->UldManager.NodeListCount <= 3) return;
            var a = (AtkComponentNode*)selectStrAddon->UldManager.NodeList[2];
            var txt = (AtkTextNode*)selectStrAddon->UldManager.NodeList[3];
            if (title == Marshal.PtrToStringUTF8((IntPtr)txt->NodeText.StringPtr))
            {
                ClickSelectString.Using(addon).SelectItem((ushort)index);
                //clickManager.SelectStringClick(addon, index);
            }
        }

        private void SelectIconString(string title)
        {
            var addon = Svc.GameGui.GetAddonByName("SelectIconString", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible) return;
            if (selectStrAddon->UldManager.NodeListCount <= 3) return;
            var a = ((AtkComponentNode*)selectStrAddon->UldManager.NodeList[2])->Component->UldManager;
            var size = a.NodeListCount;
            if (size < 12) return;
            for (var i = 1; i <= 8; i++)
            {
                var d = ((AtkComponentNode*)a.NodeList[i])->Component->UldManager;
                if (d.NodeListCount < 5) return;
                var txt = (AtkTextNode*)d.NodeList[4];
                if (title == Marshal.PtrToStringUTF8((IntPtr)txt->NodeText.StringPtr))
                {
                    if (!GenericHelpers.IsAddonReady(selectStrAddon))
                    {
                        return;
                    }
                    ClickSelectIconString.Using(addon).SelectItem((ushort)(i - 1));
                    //clickManager.SelectStringClick(addon, i-1);
                    return;
                }
            }
        }

        private void SelectYes(string title)
        {
            var addon = Svc.GameGui.GetAddonByName("SelectYesno", 1);
            if (addon == IntPtr.Zero) return;
            var selectStrAddon = (AtkUnitBase*)addon;
            if (!selectStrAddon->IsVisible) return;
            if (selectStrAddon->UldManager.NodeListCount <= 6) return;
            var a = (AtkComponentNode*)selectStrAddon->UldManager.NodeList[11];
            var txt = (AtkTextNode*)selectStrAddon->UldManager.NodeList[15];
            if (title != Marshal.PtrToStringUTF8((IntPtr)txt->NodeText.StringPtr)) return;
            if (a->Component->UldManager.NodeListCount <= 2) return;
            var b = (AtkTextNode*)a->Component->UldManager.NodeList[2];
            if ("确定" != Marshal.PtrToStringUTF8((IntPtr)b->NodeText.StringPtr)) return;
            ClickSelectYesNo.Using(addon).Yes();
            //clickManager.SendClick(addon, EventType.CHANGE, 0, ((AddonSelectYesno*)addon)->YesButton->AtkComponentBase.OwnerNode);
        }

        private void TargetByName(string name)
        {
            TaskManager.Enqueue(() =>
            {
                var Actor = Svc.Objects.Where(i => i.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc && i.Name.ToString() == name).FirstOrDefault();
                if (Actor != null)
                {
                    accessGameObject(Svc.Targets.Address, Actor.Address, (char)0);
                }
            });
        }
    }
}
