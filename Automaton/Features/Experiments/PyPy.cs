using Automaton.FeaturesSetup;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using IronPython.Hosting;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Automaton.Features.Experiments;
internal class PyPy : Feature
{
    public override string Name => "PyPy";
    public override string Description => "Run python commands";
    public override FeatureType FeatureType => FeatureType.Disabled;

    public ScriptEngine Engine { get; } = Python.CreateEngine();
    private string? Script { get; set; }
    private Dictionary<object, object> Store { get; } = [];

    private static readonly Dictionary<string, string> Commands = new()
    {
        ["/python"] = "Run a line of Python",
        ["/py"] = "Alias for /python",
        ["/pyprint"] = "Run a line of Python and print the result to the chat window",
        ["/pyadd"] = "Add a line of Python to a temporary script",
        ["/pyexecute"] = "Run the temporary script",
        ["/pyreset"] = "Clear the temporary script",
        ["/pyload"] = "Read the contents of a file relative to the config folder and run them as a Python script",
        ["/pydebug"] = "Debug printing",
    };

    private string ConfigDirectory => Path.Combine([
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XIVLauncher",
        "pluginConfigs",
        Name,
    ]);

    public override void Enable()
    {
        base.Enable();

        Engine.Runtime.Globals.SetVariable("interface", Svc.PluginInterface);
        Engine.Runtime.LoadAssembly(GetType().Assembly);
        Engine.Runtime.LoadAssembly(Assembly.GetAssembly(typeof(DalamudPluginInterface)));
        Engine.Runtime.LoadAssembly(Assembly.GetAssembly(typeof(Lumina.GameData)));
        Engine.Runtime.LoadAssembly(Assembly.GetAssembly(typeof(Lumina.Excel.ExcelRow)));
        Engine.Runtime.LoadAssembly(Assembly.GetAssembly(typeof(Lumina.Excel.GeneratedSheets.Achievement)));
        Engine.Runtime.LoadAssembly(Assembly.GetAssembly(typeof(ImGuiNET.ImGui)));

        foreach (var (name, desc) in Commands)
        {
            Svc.Commands.AddHandler(name, new CommandInfo(OnCommand)
            {
                HelpMessage = desc,
                ShowInHelp = false
            });
        }

        Directory.CreateDirectory(ConfigDirectory);
    }

    public override void Disable()
    {
        base.Disable();
        foreach (var name in Commands.Keys)
            Svc.Commands.RemoveHandler(name);

        Engine.Runtime.Shutdown();
    }

    public void OnCommand(string name, string args)
    {
        switch (name)
        {
            case "/python":
            case "/py":
            case "/pyprint":
                OneLiner(name, args);
                break;
            case "/pyadd":
                Add(args);
                break;
            case "/pyexecute":
                RunScript(args);
                break;
            case "/pyreset":
                Script = null;
                break;
            case "/pyload":
                LoadFile(args);
                break;
            case "/pydebug":
                var services = typeof(IDalamudPlugin).Assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute(typeof(PluginInterfaceAttribute)) != null)
                    .Where(t => t.Namespace != null)
                    .Select(t => $"from {t.Namespace!} import {t.Name}");
                foreach (var svc in services)
                    Svc.Log.Info(svc);
                break;
        }
    }

    private void Add(string args)
    {
        Script ??= "";
        Script += args + "\n";
    }

    private void RunScript(string args)
    {
        var script = Script;
        if (script == null)
        {
            return;
        }

        var print = args == "print";

        try
        {
            Execute(script, print);
        }
        finally
        {
            Script = null;
        }
    }

    private void Execute(string script, bool print)
    {
        var services = typeof(IDalamudPlugin).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute(typeof(PluginInterfaceAttribute)) != null)
            .Where(t => t.Namespace != null)
            .Select(t => $"from {t.Namespace!} import {t.Name}");

        var scope = Engine.CreateScope();
        scope.SetVariable("interface", Svc.PluginInterface);
        scope.SetVariable("store", Store);
        var fullScript = $@"import clr
from Automaton.Helpers.ServiceGetter import *
from Dalamud import *
from Dalamud.Plugin import *
from Dalamud.Logging import PluginLog
{string.Join('\n', services)}
from Lumina import *
from Lumina.Excel.GeneratedSheets import *
### begin custom
{script}";
        try
        {
            var result = Engine.Execute(fullScript, scope);
            if (!print)
            {
                return;
            }

            PrintModuleMessage(result.ToString());
        }
        catch (ImportException e)
        {
            Svc.Log.Error(e.Message);
        }
    }

    private void OneLiner(string name, string args)
    {
        var print = name == "/pyprint";
        Execute(args, print);
    }

    private void LoadFile(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return;
        }

        var scriptPath = Path.Combine(ConfigDirectory, args);
        var script = File.ReadAllText(scriptPath);

        Execute(script, false);
    }
}
