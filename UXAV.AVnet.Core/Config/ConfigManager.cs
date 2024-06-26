using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using UXAV.AVnet.Core.Cloud;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;
using UXAV.Logging.Console;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace UXAV.AVnet.Core.Config
{
    /// <summary>
    ///    Manages the config file for the program
    ///    <para>Config file is a json file with a schema</para>
    /// </summary>
    public static class ConfigManager
    {
        private static JToken _config;
        private static Timer _saveTimer;
        private static readonly object ConfigLockWrite = new object();
        private static readonly object PListLockWrite = new object();
        private static string _filePath;
        private static HttpClient _client;
        private static readonly Mutex PasswordMutex = new Mutex();
        private static string _plistPath;
        private static JObject _plist;

        static ConfigManager()
        {
            Logger.AddCommand(ConfigPrintToConsole, "ConfigPrint", "Print the current config");
            Logger.AddCommand(ConfigPrintInfoToConsole, "ConfigInfo",
                "Print the current config file path and last save time");
            ConfigNameSpace = Assembly.GetCallingAssembly().GetName().Name.ToLower();
            Logger.Highlight($"Config namespace is \"{ConfigNameSpace}\"");
            Logger.AddCommand((argString, args, connection, respond) => WriteCurrentConfigToDefaultPath(),
                "ConfigWriteToDefault", $"Writes current loaded config to {DefaultConfigPath}");
        }

        /// <summary>
        ///    The directory where the config file is stored
        /// </summary>
        /// <remarks>
        ///   <para>For a server, this is the program user directory <see cref="SystemBase.ProgramUserDirectory"/></para>
        ///   <para>For a processor, this is the program nvram app instance directory <see cref="SystemBase.ProgramNvramAppInstanceDirectory"/></para>
        ///  </remarks>
        public static string ConfigDirectory
        {
            get
            {
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                    return SystemBase.ProgramUserDirectory;

                return SystemBase.ProgramNvramAppInstanceDirectory;
            }
        }

        /// <summary>
        ///   The namespace of the config file
        ///   <para>Defaults to the calling assembly name in lowercase</para>
        ///  </summary>
        public static string ConfigNameSpace { get; }

        /// <summary>
        ///   The default config file path
        ///   <para>Defaults to the calling assembly name</para>
        ///   <para>Will be in the config directory with the namespace and "config.json" appended</para>
        ///   <example>
        ///   <code>
        ///     /NVRAM/app_01/myapp.config.json
        ///   </code>
        ///   </example>
        /// </summary>
        public static string DefaultConfigPath
        {
            get
            {
                var path = ConfigDirectory + "/";
                if (!string.IsNullOrEmpty(ConfigNameSpace)) path = path + ConfigNameSpace + ".";

                path += "config.json";
                return path;
            }
        }

        private static Regex FilePattern => new Regex($"(?:(\\w+)\\.)?{ConfigNameSpace}\\.config\\.json");

        /// <summary>
        ///     The current config file path
        ///     <para>Will be in the config directory with the namespace and "config.json" appended</para>
        ///     <para>Will be set to the default path if no path is set</para>
        ///     <para>Use <seealso cref="SetConfigPath" /> to change the current config file path</para>
        /// </summary>
        public static string ConfigPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_filePath)) return _filePath;

                _filePath = DefaultConfigPath;

                var configInfoFilePath = ConfigDirectory + $"/{ConfigNameSpace}.configfile.info";
                if (!File.Exists(configInfoFilePath))
                {
                    Logger.Log($"No config info found at: {configInfoFilePath}");
                    if (File.Exists(ConfigDirectory + "/configfile.info"))
                    {
                        _filePath = File.ReadAllText(ConfigDirectory + "/configfile.info");
                        if (Regex.IsMatch(_filePath, @"\/(?:\w+\.)?" + ConfigNameSpace.ToLower() + @"\."))
                        {
                            Logger.Warn(
                                "Old style info file found with relevant namespace content, will convert and remove");
                            File.Delete(ConfigDirectory + "/configfile.info");
                        }
                    }

                    File.WriteAllText(ConfigDirectory + $"/{ConfigNameSpace}.configfile.info", _filePath);
                    return _filePath;
                }

                _filePath = File.ReadAllText(configInfoFilePath);
                Logger.Log($"Config file path loaded is: {_filePath}");

                if (string.IsNullOrEmpty(_filePath))
                {
                    Logger.Warn($"Config file path loaded is invalid. Setting default: {DefaultConfigPath}");
                    _filePath = DefaultConfigPath;
                }

                return _filePath;
            }
            private set
            {
                _filePath = value;
                File.WriteAllText(ConfigDirectory + $"/{ConfigNameSpace}.configfile.info", value);
            }
        }

        private static string PListPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_plistPath)) return _plistPath;
                _plistPath = ConfigDirectory + $"/{ConfigNameSpace}.plist.json";
                return _plistPath;
            }
        }

        /// <summary>
        ///   Returns true if the current config file is the default file
        /// </summary>
        public static bool ConfigIsDefaultFile
        {
            get
            {
                var file = new FileInfo(ConfigPath).Name;
                var match = FilePattern.Match(file);
                return match.Success && !match.Groups[1].Success;
            }
        }

        private static string ConfigData
        {
            get
            {
                if (File.Exists(ConfigPath))
                {
                    Logger.Log("Config file exists at \"{0}\", getting contents", ConfigPath);
                    return File.ReadAllText(ConfigPath);
                }

                Logger.Warn("Config file not found at \"{0}\", returning null", ConfigPath);
                return null;
            }
            set
            {
                lock (ConfigLockWrite)
                {
                    try
                    {
                        Logger.Log("Writing config file at \"{0}\"", ConfigPath);
                        File.WriteAllText(ConfigPath, value);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Could not write config file, {0}", e.Message);
                    }
                }
            }
        }

        /// <summary>
        ///     Config file contents as string
        /// </summary>
        private static string PListData
        {
            get
            {
                if (File.Exists(PListPath))
                {
                    Logger.Log("plist file exists at \"{0}\", getting contents", PListPath);
                    return File.ReadAllText(PListPath);
                }

                Logger.Warn("plist file not found at \"{0}\", returning null", PListPath);
                return null;
            }
            set
            {
                lock (PListLockWrite)
                {
                    try
                    {
                        Logger.Log("Writing plist file at \"{0}\"", PListPath);
                        File.WriteAllText(PListPath, value);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Could not write plist file, {0}", e.Message);
                    }
                }
            }
        }

        /// <summary>
        ///     Get or set the current config data
        /// </summary>
        internal static JToken JConfig
        {
            get
            {
                if (_config != null) return _config;

                var configString = ConfigData;

                if (configString == null)
                {
                    Logger.Warn("Config does not exist. Returning null");
                    return null;
                }

                try
                {
                    _config = JToken.Parse(configString);
                    return _config;
                }
                catch
                {
                    Logger.Warn("ConfigText did not parse. Format error?");
                    return null;
                }
            }
            set
            {
                _config = value;
                SaveConfig();
                EventService.Notify(EventMessageType.ConfigChanged);
            }
        }

        /// <summary>
        ///    Get or the current config data schema
        /// </summary>
        public static JSchema Schema { get; private set; }

        internal static JObject PropertyList
        {
            get
            {
                if (_plist != null) return _plist;

                var data = PListData;

                if (data == null)
                {
                    var config = JConfig;
                    if (config["PropertyList"] != null)
                    {
                        Logger.Warn("Found plist data in config!");
                        var plist = config["PropertyList"] as JObject;
                        config["PropertyList"].Parent?.Remove();
                        JConfig = config;
                        if (plist != null)
                        {
                            _plist = plist;
                            SavePList(true);
                            return plist;
                        }
                    }

                    Logger.Warn("PropertyList does not exist. Creating one");
                    _plist = new JObject();
                    SavePList(true);
                    return _plist;
                }

                try
                {
                    _plist = JObject.Parse(data);
                    return _plist;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                Logger.Warn("Failed to load PList, will return empty contents");
                return new JObject();
            }
        }

        /// <summary>
        ///     Last revision (write time) of the current config file specified by <seealso cref="ConfigPath" />
        /// </summary>
        public static DateTime LastRevisionTime =>
            File.Exists(ConfigPath) ? File.GetLastWriteTime(ConfigPath) : new DateTime();

        private static IReaderConfiguration ReaderConfiguration
        {
            get
            {
                return new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    PrepareHeaderForMatch = header =>
                        CultureInfo.CurrentCulture.TextInfo
                            .ToTitleCase(Regex.Replace(header.Header, @"[\W_]", " ").ToLower())
                            .Replace(" ", string.Empty)
                };
            }
        }

        /// <summary>
        ///    Get the current config as a specific type which inherits from <see cref="ConfigBase" />
        /// </summary>
        /// <typeparam name="T">The type of the config which should be of base type ConfigBase</typeparam>
        public static T GetConfig<T>() where T : ConfigBase, new()
        {
            if (Schema == null)
            {
                var generator = new JSchemaGenerator { DefaultRequired = Required.DisallowNull };
                generator.GenerationProviders.Add(new StringEnumGenerationProvider());
                Schema = generator.Generate(typeof(T));
            }

            var data = JConfig;
            if (data == null)
            {
                Logger.Warn("Config is null. Creating default config of type: {0}", typeof(T).FullName);
                var newConfig = CreateDefaultConfig<T>();
                JConfig = JToken.FromObject(newConfig);
                return newConfig;
            }

            return JConfig.ToObject<T>();
        }

        private static T CreateDefaultConfig<T>() where T : ConfigBase, new()
        {
            var newConfig = new T();
            newConfig.CreateDefault();
            return newConfig;
        }

        /// <summary>
        ///    Set and save an updated config to the current config file
        /// </summary>
        /// <param name="config">The config class to save</param>
        public static void SetConfig(ConfigBase config)
        {
            Logger.Highlight(nameof(SetConfig));
            JConfig = JToken.FromObject(config);
        }

        /// <summary>
        ///   Set and save an updated config to a specific file
        /// </summary>
        /// <param name="config">The config class to save</param>
        /// <param name="filePath">The file path of the config file to use</param>
        /// <remarks>
        ///   Note the current file path will not be changed if saving to another file
        /// </remarks>
        public static void SetConfig(ConfigBase config, string filePath)
        {
            Logger.Highlight(nameof(SetConfig));
            var data = JToken.FromObject(config);
            lock (ConfigLockWrite)
            {
                try
                {
                    Logger.Log("Writing config file at \"{0}\"", filePath);
                    File.WriteAllText(filePath, data.ToString(Formatting.Indented));
                }
                catch (Exception e)
                {
                    Logger.Error("Could not write config file, {0}", e.Message);
                }
            }

            if (filePath == ConfigPath) _config = null;
        }

        /// <summary>
        ///   Save the current config to the default file
        /// </summary>
        /// <exception cref="Exception">
        ///   Thrown if could not write to the default config file
        /// </exception>
        public static void WriteCurrentConfigToDefaultPath()
        {
            if (_filePath == DefaultConfigPath) throw new Exception("Cannot write from default config");
            Logger.Highlight($"Writing config from {_filePath} to default path {DefaultConfigPath}");
            lock (ConfigLockWrite)
            {
                try
                {
                    Logger.Log("Writing config file at \"{0}\"", DefaultConfigPath);
                    File.WriteAllText(DefaultConfigPath, _config.ToString(Formatting.Indented));
                }
                catch (Exception e)
                {
                    Logger.Error("Could not write config file, {0}", e.Message);
                }
            }
        }

        /// <summary>
        ///   Get the details of all the config files in the config directory
        /// </summary>
        /// <return>An array of <see cref="ConfigFileDetails" /> objects</return>
        public static ConfigFileDetails[] GetFileDetails()
        {
            var directory = new DirectoryInfo(ConfigDirectory);
            var files = new List<ConfigFileDetails>();
            foreach (var file in directory.GetFiles())
            {
                var match = FilePattern.Match(file.Name);
                if (!match.Success) continue;
                var isDefault = false;
                var name = match.Groups[1].Value;
                if (string.IsNullOrEmpty(name))
                {
                    isDefault = true;
                    name = "default";
                }

                name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);

                files.Add(new ConfigFileDetails(name, file.FullName, isDefault, file.CreationTimeUtc,
                    file.LastWriteTimeUtc));
            }

            return files.OrderByDescending(f => f.CreationDate).ToArray();
        }

        /// <summary>
        ///   Set the current config file to a specific file path which then
        ///   will null the current config causing it to load from this new path
        ///   on next access using <seealso cref="GetConfig" />
        /// </summary>
        /// <param name="filePath"></param>
        public static void SetConfigPath(string filePath)
        {
            try
            {
                Logger.Highlight("Setting config file path to: " + filePath);
                ConfigPath = filePath;
                _config = null;
            }
            catch (Exception e)
            {
                ConfigPath = ConfigDirectory + "/config.json";
                Logger.Error(e);
            }
        }

        /// <summary>
        ///   Create a new config file with a specific name
        /// </summary>
        /// <param name="configName">The name of the config, not the file path. Path will be generated dynamically</param>
        /// <exception cref="InvalidOperationException">Thrown if config already exists</exception>
        public static void CreateNewFileWithName(string configName)
        {
            var name = Regex.Replace(configName, " ", "_");
            if (name.ToLower() == "default")
                throw new InvalidOperationException("Cannot have a file with the name " + configName);
            name = configName.ToLower() + (string.IsNullOrEmpty(ConfigNameSpace) ? "" : "." + ConfigNameSpace) +
                   ".config.json";
            var path = ConfigDirectory + "/" + name;
            if (File.Exists(path)) throw new InvalidOperationException("File already exists called: " + path);
            ConfigPath = path;
            SaveConfig();
        }

        /// <summary>
        ///   Delete the current config file
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the current config file is the default file
        /// </exception>
        public static void DeleteCurrentFile()
        {
            if (ConfigIsDefaultFile) throw new InvalidOperationException("You cannot delete the default config file");
            File.Delete(ConfigPath);
            SetConfigPath(GetFileDetails().First().Filepath);
        }

        /// <summary>
        ///     Get the config file as a stream
        /// </summary>
        /// <returns>The stream of the current config file</returns>
        internal static Stream GetConfigStream()
        {
            return !File.Exists(ConfigPath) ? null : File.OpenRead(ConfigPath);
        }

        /// <summary>
        ///     Get a PList item object from the current config defined by a string key name
        /// </summary>
        /// <param name="key">Unique key name for the item</param>
        /// <returns>The object defined by the key</returns>
        public static object GetPropertyListItemWithKey(string key)
        {
            // ReSharper disable once PossibleNullReferenceException
            return !PropertyList.ContainsKey(key) ? null : PropertyList[key].ToObject<object>();
        }

        /// <summary>
        ///    Get a PList item object from the current config defined by a string key name
        /// </summary>
        /// <typeparam name="T">The type of the default value</typeparam>
        /// <param name="key">Unique key name for the item</param>
        /// <param name="defaultValue">The default value stored and returned if it does not exist</param>
        /// <returns></returns>
        public static T GetOrCreatePropertyListItem<T>(string key, T defaultValue)
        {
            try
            {
                if (PropertyList.TryGetValue(key, out var value))
                    //Logger.Debug("Getting property list item with key: {0}, {1}", key, value);
                    return value.ToObject<T>();
            }
            catch (Exception e)
            {
                Logger.Error("Could not get property list item with key: {0}, {1}", key, e.Message);
                throw;
            }

            Logger.Log($"Creating new property list item with key: {key} and default value: \"{defaultValue}\"");
            PropertyList[key] = new JValue(defaultValue);
            SavePlistAuto(2);
            return defaultValue;
        }

        /// <summary>
        ///     Get a PList item object from the current config defined by a string key name
        /// </summary>
        /// <param name="key">Unique key name for the item</param>
        /// <returns>The object defined by the key</returns>
        public static string GetPropertyListStringWithKey(string key)
        {
            if (!PropertyList.ContainsKey(key))
            {
                SetPropertyListItemWithKey(key, string.Empty);
                return string.Empty;
            }

            var item = (string)GetPropertyListItemWithKey(key);
            if (item != null) return item;
            SetPropertyListItemWithKey(key, string.Empty);
            return string.Empty;
        }

        /// <summary>
        ///    Get a dynamic object from a csv file in the cloud
        /// </summary>
        /// <param name="url">The url of the csv file to get</param>
        /// <returns>A dynamic data type of the values in the csv path</returns>
        public static async Task<IEnumerable<dynamic>> GetCloudCsvDataAsync(string url)
        {
            Logger.Debug($"Getting cloud template data from: {url}");
            _client ??= new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var stream = await _client.GetStreamAsync(url);
            var reader = new StreamReader(stream);
            var csv = new CsvReader(reader, ReaderConfiguration);
            return csv.GetRecords<dynamic>();
        }

        /// <summary>
        ///   Get a dynamic object from a csv file
        /// </summary>
        /// <param name="filePath">The file path of the csv file</param>
        /// <returns>A dynamic data type of the values in the csv file</returns>
        public static IEnumerable<dynamic> GetCsvData(string filePath)
        {
            Logger.Debug($"Getting template data from: {filePath}");
            var stream = File.OpenRead(filePath);
            var reader = new StreamReader(stream);
            var csv = new CsvReader(reader, ReaderConfiguration);
            return csv.GetRecords<dynamic>();
        }

        /// <summary>
        ///     Add or set an object in the Config PList by a key name
        /// </summary>
        /// <param name="key">Unique key name for the item</param>
        /// <param name="item">The object to be defined by the key</param>
        /// <remarks>The config file will auto save 2 seconds after the last call by this method</remarks>
        public static void SetPropertyListItemWithKey(string key, object item)
        {
            lock (PropertyList)
            {
                PropertyList[key] = new JValue(item);
            }

            SavePlistAuto(2);
        }

        /// <summary>
        ///     Check if the PList contains anything by a key value
        /// </summary>
        /// <param name="key">Unique key name for the item</param>
        /// <returns>True if the PList contains item defined by key</returns>
        public static bool PropertyListContainsKey(string key)
        {
            return PropertyList.ContainsKey(key);
        }

        private static void SavePlistAuto(int seconds)
        {
            if (_saveTimer == null)
                _saveTimer = new Timer(state =>
                {
                    Logger.Highlight(1, "Plist save timer now saving file");
                    SavePList();
                });

            _saveTimer.Change(TimeSpan.FromSeconds(seconds), TimeSpan.Zero);
        }

        private static void SaveConfig()
        {
            Logger.Log("Saving config!");
            if (_config == null) return;
            ConfigData = _config.ToString(Formatting.Indented);
            _config = null;
            CloudConnector.MarkConfigForUpload();
        }

        private static void SavePList(bool keepInMemory = false)
        {
            Logger.Log("Saving plist!");
            if (_plist == null) return;
            PListData = _plist.ToString(Formatting.Indented);
            if (keepInMemory) return;
            _plist = null;
        }

        private static string[] GetPasswordKeyValues()
        {
            CrestronDataStoreStatic.GetLocalStringValue("passwordKeys", out var keysString);
            //Logger.Debug($"Password keys string = {keysString}");
            return keysString == null ? new string[] { } : keysString.Split(',');
        }

        private static void AddPasswordKeyValue(string keyValue)
        {
            var keys = GetPasswordKeyValues().ToList();
            if (keys.Contains(keyValue)) return;
            keys.Add(keyValue);
            var keysString = string.Join(",", keys);
            //Logger.Debug($"Password keys string = {keysString}");
            CrestronDataStoreStatic.SetLocalStringValue("passwordKeys", keysString);
        }

        private static void RemovePasswordKeyValue(string keyValue)
        {
            var keys = GetPasswordKeyValues().ToList();
            if (!keys.Contains(keyValue)) return;
            keys.Remove(keyValue);
            var keysString = string.Join(",", keys);
            //Logger.Debug($"Password keys string = {keysString}");
            CrestronDataStoreStatic.SetLocalStringValue("passwordKeys", keysString);
        }

        internal static System.Collections.ObjectModel.ReadOnlyDictionary<string, string> PasswordsGetAll()
        {
            try
            {
                PasswordMutex.WaitOne();
                var results = new Dictionary<string, string>();
                var keys = GetPasswordKeyValues();
                foreach (var key in keys)
                    try
                    {
                        results[key] = PasswordGet(key);
                    }
                    catch (KeyNotFoundException)
                    {
                        RemovePasswordKeyValue(key);
                    }

                return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(results);
            }
            finally
            {
                PasswordMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///   Get a password from the current config / secure storage defined by a string key name
        ///   <para>Will use secure storage if supported, otherwise will use the config plist</para>
        /// </summary>
        /// <param name="passwordKey">The key name of the password</param>
        /// <returns>The string value of the password</returns>
        /// <exception cref="KeyNotFoundException">The key does not exist</exception>
        /// <exception cref="Exception">An error thrown retrieving the value</exception>
        public static string PasswordGet(string passwordKey)
        {
            if (!CrestronSecureStorage.Supported)
            {
                Logger.Warn("Firmware does not support CrestronSecureStorage, will use config file plist!");
                return GetPropertyListStringWithKey("pw_" + passwordKey);
            }

            try
            {
                PasswordMutex.WaitOne();

                var getResult = CrestronSecureStorage.Retrieve(passwordKey, false, null, out var password);
                if (getResult == eCrestronSecureStorageStatus.RetrieveFailure)
                    throw new KeyNotFoundException($"No password stored with key \"{passwordKey}\"");

                if (getResult != eCrestronSecureStorageStatus.Ok)
                    throw new Exception($"Could not read from {nameof(CrestronSecureStorage)}, result = {getResult}");

                return Encoding.UTF8.GetString(password, 0, password.Length);
            }
            finally
            {
                PasswordMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///   As <seealso cref="PasswordGet" /> but will save and return a default value if the key does not exist
        ///   <para>Will use secure storage if supported, otherwise will use the config plist</para>
        /// </summary>
        /// <param name="passwordKey">The key name of the password</param>
        /// <param name="defaultValue">The default value to be saved and returned if the value is not already stored</param>
        /// <returns>The string value of the password</returns>
        public static string PasswordGetOrCreate(string passwordKey, string defaultValue)
        {
            if (!CrestronSecureStorage.Supported)
            {
                Logger.Warn("Firmware does not support CrestronSecureStorage, will use config file plist!");
                return GetOrCreatePropertyListItem("pw_" + passwordKey, defaultValue);
            }

            var password = defaultValue;
            try
            {
                PasswordMutex.WaitOne();
                var result = CrestronSecureStorage.Retrieve(passwordKey, false, null, out var passwordBytes);

                if (result == eCrestronSecureStorageStatus.Ok)
                    return Encoding.UTF8.GetString(passwordBytes, 0, passwordBytes.Length);

                Logger.Warn($"Password not found, result = {result}, creating new storage entry...");
                PasswordSet(passwordKey, password);
                return password;
            }
            finally
            {
                PasswordMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///   Set a password to the current config / secure storage defined by a string key name
        /// </summary>
        /// <param name="passwordKey">The key name of the password</param>
        /// <param name="value">The new value of the password to store</param>
        /// <exception cref="Exception">Thrown if the password could not be stored</exception>
        public static void PasswordSet(string passwordKey, string value)
        {
            if (!CrestronSecureStorage.Supported)
            {
                Logger.Warn("Firmware does not support CrestronSecureStorage, will use config file plist!");
                SetPropertyListItemWithKey("pw_" + passwordKey, value);
                return;
            }

            try
            {
                PasswordMutex.WaitOne();

                AddPasswordKeyValue(passwordKey);
                //Logger.Debug($"Trying to set password with key: {passwordKey}, and value: {value}");

                // ReSharper disable once UnusedVariable
                var deleteResult = CrestronSecureStorage.Delete(passwordKey, false);
                //Logger.Debug($"Delete result = {deleteResult}");
                var setResult = CrestronSecureStorage.Store(passwordKey, false, Encoding.UTF8.GetBytes(value), null);
                //Logger.Debug($"Set result = {setResult}");
                if (setResult != eCrestronSecureStorageStatus.Ok)
                    throw new Exception(
                        $"Could not store value to {nameof(CrestronSecureStorage)}, result = {setResult}");
            }
            finally
            {
                PasswordMutex.ReleaseMutex();
            }
        }

        private static void ConfigPrintInfoToConsole(string argString,
            System.Collections.ObjectModel.ReadOnlyDictionary<string, string> args, ConsoleConnection connection,
            CommandResponseAction response)
        {
            var config = JConfig;
            var type = "Unknown";
            var name = "Not set";
            if (config != null)
            {
                type = config["SystemType"]?.Value<string>();
                name = config["ConfigName"]?.Value<string>();
            }

            response($"File path: {ConfigPath}  Last revision: {LastRevisionTime}\r\n" +
                     $"System Type: {type}  System Name: {name}\r\n");
        }

        private static void ConfigPrintToConsole(string argString,
            System.Collections.ObjectModel.ReadOnlyDictionary<string, string> args, ConsoleConnection connection,
            CommandResponseAction response)
        {
            response($"Current config:\r\n{JConfig.ToString(Formatting.Indented)}\r\n");
        }
    }

    /// <summary>
    ///   Details of a config file
    /// </summary>
    public class ConfigFileDetails
    {
        internal ConfigFileDetails(string name, string filepath, bool isDefault, DateTime creationDate,
            DateTime modifiedDate)
        {
            Name = name;
            Filepath = filepath;
            IsDefault = isDefault;
            CreationDate = creationDate;
            ModifiedDate = modifiedDate;
        }

        /// <summary>
        ///  The name of the config
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///   The file path of the config
        /// </summary>
        public string Filepath { get; }

        /// <summary>
        ///   True if the config is the default file
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        ///  The creation date of the config file
        /// </summary>
        public DateTime CreationDate { get; }

        /// <summary>
        ///   The last modified date of the config file
        /// </summary>
        public DateTime ModifiedDate { get; }

        /// <summary>
        ///   True if the config file is the active file
        ///   <para>Active file is the file currently set as the ConfigPath <see cref="ConfigManager.ConfigPath"/></para>
        /// </summary>
        public bool ActiveFile => Filepath == ConfigManager.ConfigPath;
    }
}