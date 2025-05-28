using FlowSynx.PluginCore;

namespace FlowPack;

public interface IPluginLoader: IDisposable
{
    IPlugin Plugin { get; }
    void Load();
    void Unload();
}