using Automaton.FeaturesSetup;
using Dalamud.Hooking;
using Dalamud.Memory;
using ECommons;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using System;
using System.Linq;

namespace Automaton.Features.Actions;
internal unsafe class CastAndMove : Feature
{
    public override string Name => "Cast and Move";
    public override string Description => "Allows movement while casting. May result in death.";
    public override FeatureType FeatureType => FeatureType.Disabled;

    private const string PacketDispatcher_OnReceivePacketHookSig = "40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 8B F2";
    internal delegate void PacketDispatcher_OnReceivePacket(nint a1, uint a2, nint a3);
    [EzHook(PacketDispatcher_OnReceivePacketHookSig, false)]
    internal EzHook<PacketDispatcher_OnReceivePacket> PacketDispatcher_OnReceivePacketHook;
    [EzHook(PacketDispatcher_OnReceivePacketHookSig, false)]
    internal EzHook<PacketDispatcher_OnReceivePacket> PacketDispatcher_OnReceivePacketMonitorHook;

    internal delegate byte PacketDispatcher_OnSendPacket(nint a1, nint a2, nint a3, byte a4);
    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 70 8B 81 ?? ?? ?? ??", false)]
    internal EzHook<PacketDispatcher_OnSendPacket> PacketDispatcher_OnSendPacketHook;

    internal delegate bool UseActionDelegate(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8);
    internal Hook<UseActionDelegate> UseActionHook;

    internal DelayedAction DelayedAction = null;

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        try
        {
            //ImGui.TextUnformatted($"receive: {(PacketDispatcher_OnReceivePacketHook.IsEnabled ? "on" : "off")}, send {(PacketDispatcher_OnSendPacketHook.IsEnabled ? "on" : "off")}");
            if (ImGui.Button("toggle receive"))
                TogglePacketReceiveHook();
            if (ImGui.Button("toggle send"))
                TogglePacketSendHook();
        }
        catch (Exception e)
        {
            e.Log();
        }
    };

    public override void Enable()
    {
        base.Enable();
        //UseActionHook = Svc.Hook.HookFromAddress<UseActionDelegate>((nint)ActionManager.Addresses.UseAction.Value, UseActionDetour);
        //Svc.Hook.InitializeFromAttributes(this);
        PacketDispatcher_OnReceivePacketHook.Enable();
        PacketDispatcher_OnSendPacketHook.Enable();
    }

    public override void Disable()
    {
        base.Disable();
        PacketDispatcher_OnReceivePacketHook.Pause();
        PacketDispatcher_OnSendPacketHook.Pause();
        //UseActionHook.Disable();
    }

    private void TogglePacketReceiveHook()
    {
        if (PacketDispatcher_OnReceivePacketHook.IsEnabled)
            PacketDispatcher_OnReceivePacketHook.Pause();
        else
            PacketDispatcher_OnReceivePacketHook.Enable();
    }
    private void TogglePacketSendHook()
    {
        if (PacketDispatcher_OnSendPacketHook.IsEnabled)
            PacketDispatcher_OnSendPacketHook.Pause();
        else
            PacketDispatcher_OnSendPacketHook.Enable();
    }

    private void PacketDispatcher_OnReceivePacketMonitorDetour(nint a1, uint a2, nint a3)
    {
        PacketDispatcher_OnReceivePacketMonitorHook.Original(a1, a2, a3);
        try
        {
            var opcode = *(ushort*)(a3 + 2);
            var dataPtr = a3 + 16;
            if (opcode == 0xE2)
            {
                var acopcode = *(ushort*)dataPtr;
                var data = "";
                try
                {
                    data = $"{MemoryHelper.ReadRaw(dataPtr + 4, 28).Select(x => $"{x:X2}").Print(" ")}";
                }
                catch { }
                Svc.Log.Debug($"ActorControl: {acopcode} / {data}"); //
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    private byte PacketDispatcher_OnSendPacketDetour(nint a1, nint a2, nint a3, byte a4)
    {
        const byte DefaultReturnValue = 1;

        if (a2 == IntPtr.Zero)
        {
            Svc.Log.Error("[HyperFirewall] Error: Opcode pointer is null.");
            return DefaultReturnValue;
        }

        try
        {
            var opcode = *(ushort*)a2;

            switch (opcode)
            {
                case 499:
                    Svc.Log.Verbose($"[HyperFirewall] Passing outgoing packet with opcode {opcode} through.");
                    return PacketDispatcher_OnSendPacketHook.Original(a1, a2, a3, a4);

                default:
                    Svc.Log.Verbose($"[HyperFirewall] Suppressing outgoing packet with opcode {opcode}.");
                    break;
            }
        }
        catch (Exception e)
        {
            Svc.Log.Error($"[HyperFirewall] Exception caught while processing opcode: {e.Message}");
            e.Log();
            return DefaultReturnValue;
        }

        return DefaultReturnValue;
    }

    private void PacketDispatcher_OnReceivePacketDetour(nint a1, uint a2, nint a3)
    {
        if (a3 == IntPtr.Zero)
        {
            Svc.Log.Error("[HyperFirewall] Error: Data pointer is null.");
            return;
        }

        try
        {
            var opcode = *(ushort*)(a3 + 2);

            switch (opcode)
            {
                case 593:
                case 660:
                    Svc.Log.Verbose($"[HyperFirewall] Passing incoming packet with opcode {opcode} through.");
                    PacketDispatcher_OnReceivePacketHook.Original(a1, a2, a3);
                    return;

                default:
                    Svc.Log.Verbose($"[HyperFirewall] Suppressing incoming packet with opcode {opcode}.");
                    break;
            }
        }
        catch (Exception e)
        {
            Svc.Log.Error($"[HyperFirewall] Exception caught while processing opcode: {e.Message}");
            e.Log();
            return;
        }

        return;
    }

    private bool UseActionDetour(ActionManager* am, ActionType type, uint acId, long target, uint a5, uint a6, uint a7, void* a8)
    {
        if (Enabled)
        {
            try
            {
                InternalLog.Verbose($"{type}, {acId}, {target}");
                if (DelayedAction == null && ((type == ActionType.Action && IsActionCastable(acId)) || type == ActionType.Mount) && GCD == 0 && AgentMap.Instance()->IsPlayerMoving != 0 && !am->ActionQueued)
                {
                    DelayedAction = new(acId, type, 0, target, a5, a6, a7, a8);
                    return false;
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
        }
        var ret = UseActionHook.Original(am, type, acId, target, a5, a6, a7, a8);
        return ret;
    }

    internal static bool IsActionCastable(uint id)
    {
        var actionSheet = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>();
        id = ActionManager.Instance()->GetAdjustedActionId(id);
        var actionRow = actionSheet.GetRow(id);

        if (actionRow?.Cast100ms <= 0)
        {
            return false;
        }

        var actionManager = ActionManager.Instance();
        var adjustedCastTime = ActionManager.GetAdjustedCastTime(ActionType.Action, id);

        return adjustedCastTime > 0;
    }

    internal static float GCD
    {
        get
        {
            var cd = ActionManager.Instance()->GetRecastGroupDetail(57);
            return cd->IsActive == 0 ? 0 : cd->Total - cd->Elapsed;
        }
    }
}

internal unsafe class DelayedAction
{
    internal uint actionId = 0;
    internal long execAt = 0;
    internal long targetId = 0;
    internal uint a5, a6, a7;
    internal void* a8;
    internal ActionType type;

    internal DelayedAction(uint actionId, ActionType type, long execAt, long targetId, uint a5, uint a6, uint a7, void* a8)
    {
        this.actionId = actionId;
        this.execAt = execAt;
        this.targetId = targetId;
        this.a5 = a5;
        this.a6 = a6;
        this.a7 = a7;
        this.a8 = a8;
        this.type = type;
        PluginLog.Debug($"Generated delayed action: {this}");
    }

    internal void Use() => _ = new CastAndMove().UseActionHook.Original.Invoke(ActionManager.Instance(), type, actionId, targetId, a5, a6, a7, a8);

    public override string ToString() => $"[id={actionId}, type={type}, execAt={execAt}, target={targetId:X16}, a5={a5}, a6={a6}, a7={a7}, a8={(nint)a8:X16}]";
}
