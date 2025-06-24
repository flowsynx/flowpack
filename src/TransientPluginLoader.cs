﻿using FlowSynx.PluginCore;
using System.Reflection;

namespace FlowPack;

public class TransientPluginLoader : IPluginLoader
{
    private readonly string _pluginLocation;
    private WeakReference? _contextWeakRef;
    private IPlugin? _pluginInstance;
    private bool _isUnloaded;
    public IPlugin Plugin => _pluginInstance ?? throw new ObjectDisposedException(nameof(TransientPluginLoader));

    public TransientPluginLoader(string pluginLocation)
    {
        ValidatePluginFile(pluginLocation);
        _pluginLocation = pluginLocation;
    }

    public void Load()
    {
        using (var context = new PluginLoadContext(_pluginLocation))
        {
            _contextWeakRef = new WeakReference(context, true);
            var pluginAssembly = context.LoadFromAssemblyPath(_pluginLocation);
            var pluginType = LoadPluginType(pluginAssembly);

            if (pluginType == null)
                throw new Exception($"No plugin type found: '{_pluginLocation}'");

            _pluginInstance = CreatePluginInstance(pluginType);
        }
    }

    private void ValidatePluginFile(string pluginLocation)
    {
        if (!File.Exists(pluginLocation))
        {
            throw new Exception($"Plugin file not found: {pluginLocation}");
        }
    }

    private Type? LoadPluginType(Assembly pluginAssembly)
    {
        var interfaceType = typeof(IPlugin);
        return pluginAssembly
            .GetTypes()
            .FirstOrDefault(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
    }

    private IPlugin CreatePluginInstance(Type pluginType)
    {
        if (Activator.CreateInstance(pluginType) is not IPlugin instance)
        {
            throw new Exception($"Failed to create plugin instance of type '{pluginType.FullName}'.");
        }
        return instance;
    }

    public void Unload()
    {
        if (_isUnloaded)
            return;

        SafeUnload();
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    private void SafeUnload()
    {
        try
        {
            _pluginInstance = null;
            _isUnloaded = true;

            if (_contextWeakRef == null) 
                return;

            for (var i = 0; _contextWeakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        catch
        {
            // Swallow unload exceptions to not mask the original exception
        }
    }
}