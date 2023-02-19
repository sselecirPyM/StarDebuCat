using MilkWangBase.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MilkWangBase.Core;

public class Fusion : IDisposable
{
    public List<object> systems = new();
    public Dictionary<Type, object> systems1 = new();
    public List<(object, MethodInfo)> updates = new();
    public List<(object, MethodInfo)> lateUpdates = new();

    public class DiffusionInfo
    {
        public FieldInfo source;
        public List<(object, FieldInfo)> diffusinTargets = new();
    }

    public class XDiffusionInfo
    {
        public MethodInfo source;
        public List<(object, FieldInfo)> diffusinTargets = new();
    }

    public Dictionary<string, DiffusionInfo> diffusions = new();
    public Dictionary<string, XDiffusionInfo> xdiffusions = new();
    public Dictionary<Type, List<string>> diffusions1 = new();

    public Fusion(object controller)
    {
        AddChildSystems(controller);
    }

    void AddChildSystems(object obj)
    {
        var type = obj.GetType();

        foreach (var item in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var systemAttribute = item.GetCustomAttribute<SystemAttribute>();
            if (systemAttribute != null)
            {
                object inst = item.GetValue(obj);
                if (inst == null)
                {
                    inst = Activator.CreateInstance(item.FieldType);
                    item.SetValue(obj, inst);
                }
                systems.Add(inst);
                systems1.Add(inst.GetType(), inst);
                AddChildSystems(inst);
            }
        }
    }

    public void InitializeSystems()
    {
        foreach (var system in systems)
        {
            InitializeSystem0(system);
        }

        foreach (var system in systems)
        {
            InitializeSystem1(system);
        }
    }

    void InitializeSystem0(object system)
    {
        var type = system.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        List<string> diffusionNames = new List<string>();
        foreach (var field in fields)
        {
            if (systems1.TryGetValue(field.FieldType, out var sys1))
            {
                field.SetValue(system, sys1);
            }
            var difAttr = field.GetCustomAttribute<DiffusionAttribute>();
            if (difAttr != null)
            {
                diffusions.Add(difAttr.MemberName, new DiffusionInfo() { source = field });
                diffusionNames.Add(difAttr.MemberName);
            }
        }
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var method in methods)
        {
            var methodCount = method.GetParameters().Length;
            if (method.Name == "Initialize" && methodCount == 0)
            {
                method.Invoke(system, null);
            }
            if (method.Name == "Update" && methodCount == 0)
            {
                updates.Add((system, method));
            }
            if (method.Name == "LateUpdate" && methodCount == 0)
            {
                lateUpdates.Add((system, method));
            }
            var difAttr = method.GetCustomAttribute<XDiffusionAttribute>();
            if (difAttr != null)
            {
                xdiffusions.Add(difAttr.MemberName, new XDiffusionInfo() { source = method });
                diffusionNames.Add(difAttr.MemberName);
            }
        }
        if (diffusionNames.Count > 0)
        {
            diffusions1.Add(type, diffusionNames);
        }
    }
    void InitializeSystem1(object system)
    {
        var type = system.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var findAttribute = field.GetCustomAttribute<FindAttribute>();
            if (findAttribute != null && diffusions.TryGetValue(findAttribute.MemberName, out var diffusionInfo))
            {
                diffusionInfo.diffusinTargets.Add((system, field));
            }
            var xfindAttribute = field.GetCustomAttribute<XFindAttribute>();
            if (xfindAttribute != null && xdiffusions.TryGetValue(xfindAttribute.MemberName, out var xdiffusionInfo))
            {
                xdiffusionInfo.diffusinTargets.Add((system, field));
            }
        }
    }

    void SetDiffusions(object system)
    {
        if (diffusions1.TryGetValue(system.GetType(), out var diffusionNames))
        {
            foreach (var name in diffusionNames)
            {
                _SetDiffusion(name, system);
                _SetXDiffusion(name, system);
            }
        }
    }
    void _SetDiffusion(string name, object system)
    {
        if (!diffusions.TryGetValue(name, out var info))
            return;
        var val = info.source.GetValue(system);
        foreach (var diffusion in info.diffusinTargets)
        {
            diffusion.Item2.SetValue(diffusion.Item1, val);
        }
    }
    void _SetXDiffusion(string name, object system)
    {
        if (!xdiffusions.TryGetValue(name, out var info1))
            return;
        foreach (var diffusion in info1.diffusinTargets)
        {
            info1.source.Invoke(system, new object[] { diffusion.Item1, diffusion.Item2 });
        }
    }

    public void Update()
    {
        foreach (var o in updates)
        {
            o.Item2.Invoke(o.Item1, null);
            SetDiffusions(o.Item1);
        }
        foreach (var o in lateUpdates)
        {
            o.Item2.Invoke(o.Item1, null);
        }
    }

    public void Dispose()
    {
        for (int i = systems.Count - 1; i >= 0; i--)
        {
            object system = systems[i];
            if (system is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
