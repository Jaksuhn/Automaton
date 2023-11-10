using Automaton.Debugging;
using Automaton.Helpers;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static ECommons.GenericHelpers;

namespace Automaton.Features.Debugging;

public unsafe class QuestDebug : DebugHelper
{
    public override string Name => $"{nameof(QuestDebug).Replace("Debug", "")} Debugging";

    private readonly FeatureProvider provider = new(Assembly.GetExecutingAssembly());

    private QuestManager qm = new();
    private QuestManager* _qm = QuestManager.Instance();
    private int selectedQuestID;
    private string questName = "";
    private readonly ExcelSheet<Quest> questSheet;
    private static readonly Dictionary<uint, Quest>? QuestSheet = Svc.Data?.GetExcelSheet<Quest>()?
    .Where(x => x.Id.RawString.Length > 0)
    .ToDictionary(i => i.RowId, i => i);
    private readonly List<SeString> questNames = Svc.Data.GetExcelSheet<Quest>(Svc.ClientState.ClientLanguage).Select(x => x.Name).ToList();

    public override void Draw()
    {
        ImGui.Text($"{Name}");
        ImGui.Separator();

        if (ImGui.Button("Very Easy") && TryGetAddonByName<AtkUnitBase>("DifficultySelectYesNo", out var addon))
            Callback.Fire(addon, true, 0, 2);

        ImGui.InputText("###QuestNameInput", ref questName, 500);
        if (questName != "")
        {
            var quest = TrySearchQuest(questName);
            ImGui.Text($"QuestID: {(int)quest.RowId}");
        }
        ImGui.InputInt("###QuestIDInput", ref selectedQuestID, 500);
        if ( selectedQuestID != 0 )
        {
            ImGui.Text($"Is Quest Accepted?: {qm.IsQuestAccepted((ushort)selectedQuestID)}");
            ImGui.Text($"Is Quest Complete?: {QuestManager.IsQuestComplete((ushort)selectedQuestID)}");
            ImGui.Text($"Current Quest Sequence: {QuestManager.GetQuestSequence((ushort)selectedQuestID)}");
        }

        ImGui.Separator();

        ImGuiEx.TextUnderlined("Accepted Quests");
        foreach (var quest in _qm->NormalQuestsSpan)
        {
            if (quest.QuestId is 0) continue;

            ImGui.Text($"{quest.QuestId}: {NameOfQuest(quest.QuestId)}\n   seq: {quest.Sequence} flag: {quest.Flags}");
        }
    }

    public static string NameOfQuest(ushort id)
    {
        if (id > 0)
        {
            var digits = id.ToString().Length;
            if (QuestSheet!.Any(x => Convert.ToInt16(x.Value.Id.RawString.GetLast(digits)) == id))
            {
                return QuestSheet!.First(x => Convert.ToInt16(x.Value.Id.RawString.GetLast(digits)) == id).Value.Name.RawString.Replace("", "").Trim();
            }
        }
        return "";
    }

    private Quest TrySearchQuest(string input)
    {
        var matchingRows = questNames.Select((n, i) => (n, i)).Where(t => !string.IsNullOrEmpty(t.n) && IsMatch(input, t.n)).ToList();
        if (matchingRows.Count > 1)
        {
            matchingRows = matchingRows.OrderByDescending(t => MatchingScore(t.n, input)).ToList();
        }
        return matchingRows.Count > 0 ? questSheet.GetRow((uint)matchingRows.First().i) : null;
    }

    private static bool IsMatch(string x, string y) => Regex.IsMatch(x, $@"\b{Regex.Escape(y)}\b");
    private static object MatchingScore(string item, string line)
    {
        var score = 0;

        // primitive matching based on how long the string matches. Enough for now but could need expanding later
        if (line.Contains(item))
            score += item.Length;

        return score;
    }
}
