using Automaton.FeaturesSetup;
using Automaton.Helpers;
using Automaton.UI;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using static ECommons.GenericHelpers;

namespace Automaton.Features.UI
{
    public unsafe class FelicitousTokenInMenu : Feature
    {
        public override string Name => "Show Felicitous Tokens In Menu";

        public override string Description => "Shows the amount of Felicitous Tokens you have in the main Island menu.";

        public override FeatureType FeatureType => FeatureType.Disabled;

        private Overlays overlay;
        private float height;

        private static AtkUnitBase* AddonMJIHud => Common.GetUnitBase("MJIHud");

        internal bool active = false;

        public override void Enable()
        {
            overlay = new Overlays(this);
            Svc.Framework.Update += OnUpdate;
            base.Enable();
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(overlay);
            Svc.Framework.Update -= OnUpdate;
            base.Disable();
        }

        public override void Draw()
        {
            if (TryGetAddonByName<AtkUnitBase>("MJIHud", out var addon))
            {
                return;
            }
        }

        private void OnUpdate(IFramework framework)
        {
            if (!UiHelper.IsAddonReady(AddonMJIHud)) return;

            // Button Component Node
            var currencyPositionNode = Common.GetNodeByID(&AddonMJIHud->UldManager, 3);
            if (currencyPositionNode == null) return;
            Vector2 baseIconPosition;
            var iconPosition = baseIconPosition = new Vector2(currencyPositionNode->X, currencyPositionNode->Y);
        }

        private void TryMakeIconNode(uint nodeId, Vector2 position, int icon, bool hqIcon, string? tooltipText = null)
        {
            var iconNode = Common.GetNodeByID(&AddonMJIHud->UldManager, nodeId);
            if (iconNode is null)
            {
                MakeIconNode(nodeId, position, icon, hqIcon, tooltipText);
            }
            else
            {
                iconNode->SetPositionFloat(position.X, position.Y);
            }
        }

        private void MakeIconNode(uint nodeId, Vector2 position, int icon, bool hqIcon, string? tooltipText = null)
        {
            var imageNode = UiHelper.MakeImageNode(nodeId, new UiHelper.PartInfo(0, 0, 36, 36));
            imageNode->AtkResNode.NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents;
            imageNode->WrapMode = 1;
            imageNode->Flags = (byte)ImageNodeFlags.AutoFit;

            imageNode->LoadIconTexture(hqIcon ? icon + 1_000_000 : icon, 0);
            imageNode->AtkResNode.ToggleVisibility(true);

            imageNode->AtkResNode.SetWidth(36);
            imageNode->AtkResNode.SetHeight(36);
            imageNode->AtkResNode.SetPositionShort((short)position.X, (short)position.Y);

            UiHelper.LinkNodeAtEnd((AtkResNode*)imageNode, AddonMJIHud);

            //if (!TweakConfig.DisableEvents && tooltipText != null && simpleEvent != null)
            //{
            //    imageNode->AtkResNode.NodeFlags |= NodeFlags.RespondToMouse | NodeFlags.EmitsEvents | NodeFlags.HasCollision;
            //    AddonMoney->UpdateCollisionNodeList(false);
            //    simpleEvent?.Add(AddonMoney, &imageNode->AtkResNode, AtkEventType.MouseOver);
            //    simpleEvent?.Add(AddonMoney, &imageNode->AtkResNode, AtkEventType.MouseOut);
            //    tooltipStrings.TryAdd(nodeId, tooltipText);
            //}
        }
    }
}
