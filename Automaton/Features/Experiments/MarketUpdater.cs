using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Automaton.UI;
using ClickLib.Clicks;
using ECommons.Automation;
using ECommons.Logging;
using ECommons.Throttlers;
using ECommons.UIHelpers.Implementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Numerics;
using static ECommons.GenericHelpers;

namespace Automaton.Features.Testing
{
    public unsafe class MarketUpdater : Feature
    {
        public override string Name => "Market Updater";

        public override string Description => "Penny pinches all listings on retainers";

        public override FeatureType FeatureType => FeatureType.Disabled;

        private Overlays overlay;
        private float height;

        internal bool active = false;

        public override void Enable()
        {
            overlay = new Overlays(this);
            base.Enable();
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(overlay);
            base.Disable();
        }

        public override void Draw()
        {
            if (TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon))
            {
                if (!(addon->UldManager.NodeListCount > 1)) return;
                if (!addon->UldManager.NodeList[1]->IsVisible) return;

                var node = addon->UldManager.NodeList[1];

                if (!node->IsVisible)
                    return;

                var position = AtkResNodeHelper.GetNodePosition(node);

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(addon->X, addon->Y - height));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(10f, 10f));
                ImGui.Begin($"###{Name}{node->NodeID}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

                if (ImGui.Button(!active ? $"{Name}###Start" : $"Running. Click to abort.###Abort"))
                {
                    if (!active)
                    {
                        active = true;
                        TaskManager.Enqueue(YesAlready.DisableIfNeeded);
                        TaskManager.Enqueue(() => UpdateListings((int)addon->AtkValues[2].UInt));
                    }
                    else
                    {
                        CancelLoop();
                    }
                }

                height = ImGui.GetWindowSize().Y;

                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }

        private void CancelLoop()
        {
            active = false;
            TaskManager.Abort();
            TaskManager.Enqueue(YesAlready.EnableIfNeeded);
        }

        private void UpdateListings(int numRetainers)
        {
            for (var i = 0; i < numRetainers; i++)
            {
                return;
            }
            TaskManager.Enqueue(() => active = false);
            TaskManager.Enqueue(YesAlready.EnableIfNeeded);
        }

        internal static bool GenericThrottle => FrameThrottler.Throttle("AutoRetainerGenericThrottle", 200);

        internal static bool? SelectRetainerByName(string name)
        {
            if (name.IsNullOrEmpty())
            {
                throw new Exception($"Name can not be null or empty");
            }
            if (TryGetAddonByName<AtkUnitBase>("RetainerList", out var retainerList) && IsAddonReady(retainerList))
            {
                var list = new ReaderRetainerList(retainerList);
                for (var i = 0; i < list.Retainers.Count; i++)
                {
                    if (list.Retainers[i].Name == name)
                    {
                        if (GenericThrottle)
                        {
                            PluginLog.Debug($"Selecting retainer {list.Retainers[i].Name} with index {i}");
                            ClickRetainerList.Using((nint)retainerList).Retainer(i);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
