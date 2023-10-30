using ECommons.DalamudServices;
using Automaton.FeaturesSetup;
using Automaton.Helpers;
using ImGuiNET;
using Dalamud.Game.ClientState.Keys;

namespace Automaton.Features.Actions
{
    public unsafe class ClickToMove : Feature
    {
        public override string Name => "Click to Move";

        public override string Description => "Like those other games.";

        public override FeatureType FeatureType => FeatureType.Disabled;

        public Configs Config { get; private set; }
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Distance to Keep", "", 1, IntMin = 0, IntMax = 30, EditorSize = 300)]
            public VirtualKey keybind;
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
        };
        private readonly OverrideMovement movement = new();

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += MoveTo;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);

            Svc.Framework.Update -= MoveTo;
            base.Disable();
        }

        private static bool CheckHotkeyState(VirtualKey key) => !Svc.KeyState[key];

        private void MoveTo(IFramework framework)
        {
            if (!CheckHotkeyState(VirtualKey.LBUTTON)) return;

            var mousePos = ImGui.GetIO().MousePos;
            Svc.GameGui.ScreenToWorld(mousePos, out var pos, 100000f);
            Svc.Log.Info($"m1 pressed, moving to {pos.X}, {pos.Y}, {pos.Z}");
            movement.DesiredPosition = pos;
        }
    }
}
