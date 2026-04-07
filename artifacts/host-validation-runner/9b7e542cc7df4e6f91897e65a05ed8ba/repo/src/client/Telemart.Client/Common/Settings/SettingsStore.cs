using System;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
using Telemart.Client.Core.IO;
using Telemart.Client.Core.Serialization;

namespace Telemart.Client.Common.Settings
{
    internal abstract class SettingsStore<T> : ISettingsStore<T>
        where T : class, new()
    {
        private string basePath;

        protected SettingsStore(ISerializerBuilder serializerBuilder, IFileSystem fileSystem)
        {
            SerializerBuilder = serializerBuilder ?? throw new ArgumentNullException(nameof(serializerBuilder));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public string BasePath => basePath ??= FileSystem.Path.Combine(GetBasePath(), "settings");

        public abstract string FileName { get; protected set; }

        public string FilePath => FileSystem.Path.Combine(BasePath, FileName ?? string.Empty);

        private IFileSystem FileSystem { get; }

        private ISerializerBuilder SerializerBuilder { get; }

        public async Task<T> LoadAsync()
        {
            T settings = null;

            if (FileSystem.File.Exists(FilePath))
            {
                byte[] bytes = await FileHelper.ReadBytesAsync(FilePath);
                settings = DeserializeSettings(bytes);
            }

            if (settings == null)
            {
                settings = GetDefault();
                await SaveAsync(settings);
            }
            else
            {
                settings = MergeSettings(settings, GetDefault());
            }

            await PreProcessAsync(settings);

            return settings;
        }

        public async Task SaveAsync(T obj)
        {
            CreateSavingDirectoryIfNotExists();

            await PostProcessAsync(obj);

            await FileHelper.WriteBytesAsync(FilePath, GetSerializedBytes(obj));
        }

        protected abstract string GetBasePath();

        protected virtual T MergeSettings(T settings, T defaultSettings)
        {
            return settings;
        }

        protected virtual Task PreProcessAsync(T settings)
        {
            return Task.CompletedTask;
        }

        protected virtual Task PostProcessAsync(T settings)
        {
            return Task.CompletedTask;
        }

        protected abstract T GetDefault();

        private static Encoding GetEncoding()
        {
            return Encoding.UTF8;
        }

        private void CreateSavingDirectoryIfNotExists()
        {
            if (!FileSystem.Directory.Exists(BasePath))
            {
                FileSystem.Directory.CreateDirectory(BasePath);
            }
        }

        private T DeserializeSettings(byte[] bytes)
        {
            string source = GetEncoding().GetString(bytes, 0, bytes.Length);

            ISerializer<T> serializer = SerializerBuilder.Build<T>();
            T settings = serializer.Deserialize(source);

            return settings;
        }

        private byte[] GetSerializedBytes(T obj)
        {
            ISerializer<T> serializer = SerializerBuilder.Build<T>();
            string source = serializer.Serialize(obj);
            return GetEncoding().GetBytes(source);
        }
    }
}