using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Triggers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace HomeAutomation.Core.Services
{
    public interface IJsonDatabaseService
    {
        MemoryEntities MemoryEntities { get; }

        List<Device> Devices { get; }

        List<Trigger> Triggers { get; }

        List<HomeAutomation.Entities.Action.Action> Actions { get; }

        IEnumerable<CameraDevice> Cameras { get; }

        IEnumerable<SensorDevice> Sensors { get; }

        IEnumerable<PowerSwitchDevice> PowerSwitches { get; }

        IEnumerable<ScheduleTrigger> ScheduledTriggers { get; }

        IEnumerable<DeviceTrigger> StateTriggers { get; }

        void Initialize();

        string ReadConfiguration();

        bool SaveConfiguration(string configuration, out string error);
    }

    public class JsonDatabaseService : IJsonDatabaseService
    {
        private string DATABASE_FILE = "database.json";

        private static readonly object readMemoryEntitiesLock = new();
        private static readonly object writeMemoryEntitiesLock = new();

        private FileSystemWatcher configurationWatcher;

        private MemoryEntities memoryEntities;

        public MemoryEntities MemoryEntities
        {
            get
            {
                lock (readMemoryEntitiesLock)
                {
                    return memoryEntities;
                }
            }
        }

        public List<Device> Devices => MemoryEntities.Devices;

        public List<Trigger> Triggers => MemoryEntities.Triggers;

        public List<HomeAutomation.Entities.Action.Action> Actions => MemoryEntities.Actions;

        public IEnumerable<CameraDevice> Cameras => MemoryEntities.Devices.OfType<CameraDevice>();

        public IEnumerable<SensorDevice> Sensors => MemoryEntities.Devices.OfType<SensorDevice>();

        public IEnumerable<PowerSwitchDevice> PowerSwitches => MemoryEntities.Devices.OfType<PowerSwitchDevice>();

        public IEnumerable<ScheduleTrigger> ScheduledTriggers => MemoryEntities.Triggers.OfType<ScheduleTrigger>();

        public IEnumerable<DeviceTrigger> StateTriggers => MemoryEntities.Triggers.OfType<DeviceTrigger>();

        public JsonDatabaseService(IConfiguration configuration)
        {
            DATABASE_FILE = configuration.GetConnectionString("Json") ?? DATABASE_FILE;
        }

        public void Initialize()
        {
            LoadConfiguration();

            configurationWatcher = new FileSystemWatcher();
            configurationWatcher.Path = Directory.GetCurrentDirectory();

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            configurationWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            // Only watch text files.
            configurationWatcher.Filter = DATABASE_FILE;

            // Add event handlers.
            configurationWatcher.Changed += ConfigurationWatcher_Changed;
            configurationWatcher.Created += ConfigurationWatcher_Changed;
            configurationWatcher.Deleted += ConfigurationWatcher_Changed;
            configurationWatcher.Renamed += ConfigurationWatcher_Changed;

            // Begin watching.
            configurationWatcher.EnableRaisingEvents = true;
        }

        public string ReadConfiguration()
        {
            lock (writeMemoryEntitiesLock)
            {
                return File.ReadAllText(DATABASE_FILE, Encoding.UTF8);
            }
        }

        public bool SaveConfiguration(string configuration, out string error)
        {
            lock (writeMemoryEntitiesLock)
            {
                error = null;
                try
                {
                    JsonConvert.DeserializeObject<MemoryEntities>(configuration);
                    File.WriteAllText(DATABASE_FILE, configuration, Encoding.UTF8);
                    return true;
                }
                catch (Exception ex)
                {
                    // TODO: logging?
                    error = ex.Message;
                    return false;
                }
            }
        }

        private void ConfigurationWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            lock (readMemoryEntitiesLock)
            {
                string itemJson = ReadConfiguration();
                memoryEntities = JsonConvert.DeserializeObject<MemoryEntities>(itemJson);
            }
        }
    }
}