using Automaton.Features.UI;
using Automaton.FeaturesSetup;
using ECommons.Reflection;
using System;
using System.Linq;
using System.Reflection;

namespace Automaton.Helpers;

public static class FeatureHelper
{

    internal static bool IsBusy => P.TaskManager.IsBusy || WorkshopTurnin.active;
    private static bool IsEnabled(BaseFeature feature) => Config.EnabledFeatures.Contains(feature.GetType().Name);

    public static bool IsEnabled<T>() where T : BaseFeature
    {
        var assembly = Assembly.GetExecutingAssembly();
        var t = assembly.GetTypes().Where(x => x == typeof(T)).First();
        var f = (T)Activator.CreateInstance(t);

        return IsEnabled(f);

    }

    public static void EnableFeature<T>() where T : BaseFeature
    {
        var t = Assembly.GetExecutingAssembly().GetTypes().Where(x => x == typeof(T)).First();
        var f = P.Features.Where(x => x.GetType().Name == t.Name).FirstOrDefault();

        if (f != null && !f.Enabled)
        {
            f.Enable();
        }
    }

    public static void DisableFeature<T>() where T : BaseFeature
    {
        var t = Assembly.GetExecutingAssembly().GetTypes().Where(x => x == typeof(T)).First();
        var f = P.Features.Where(x => x.GetType().Name == t.Name).FirstOrDefault();

        if (f != null && f.Enabled)
        {
            f.Disable();
        }
    }

    public static FeatureConfig GetConfig<T>() where T : BaseFeature
    {
        var t = Assembly.GetExecutingAssembly().GetTypes().Where(x => x == typeof(T)).First();
        var f = P.Features.Where(x => x.GetType().Name == t.Name).FirstOrDefault();

        if (f != null)
        {
            var config = f.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig))).GetValue(f);

            if (config != null)
            {
                return (FeatureConfig)config;
            }
        }

        return null;
    }

    public static bool? IsEnabled(this FeatureConfig config, string propname) => config.GetFoP(propname) != null ? (bool)config.GetFoP(propname) : null;

    public static void ToggleConfig(this FeatureConfig config, string propName, bool state)
    {
        if (config.GetFoP(propName) != null)
        {
            config.SetFoP(propName, state);
        }
    }
}
