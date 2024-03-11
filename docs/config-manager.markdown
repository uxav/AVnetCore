---
layout: page
title: "Config Manager"
permalink: /config
nav_order: 3
---
## Config Manager

Use the config manager to load or save a configuration for your program instance.

### Create a config class

Start by creating a class for your config that inherits from ConfigBase.

```csharp
public class SystemConfig : ConfigBase
{
    [Description("The defined rooms in the system.")]
    public IEnumerable<RoomConfig> Rooms;

    [Description("The defined sources in the system.")]
    public IEnumerable<SourceConfig> Sources;

    [Description("The IP address of the DSP to use for audio processing.")]
    public string DspAddress { get; set; }

    public override void CreateDefault()
    {
        // create the default config here which is called when the manager requests a
        // default config, say for example when it's a new program
    }
}
```

### Load the config using the ConfigManager

Load the config using config manager passing the type of the config file so it knows how to handle the underlying json.

```csharp
// load the config
var config = ConfigManager.GetConfig<SystemConfig>();

// check the file is the default path and overwrite it with a new default config
// this helps through the development cycle when creating a config class
if (ConfigManager.ConfigIsDefaultFile)
{
    Logger.Warn("Config is default, overwriting with new default config!");
    config = new SystemConfig();
    config.CreateDefault();
    ConfigManager.SetConfig(config, ConfigManager.DefaultConfigPath);
}
```

### Saving the config

Save the config to the current config file

```csharp
ConfigManager.SetConfig(myConfig);
```