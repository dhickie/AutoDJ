using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public class PersistenceService : IPersistenceService
    {
        private readonly SemaphoreSlim _indexFileLock;
        private readonly SemaphoreSlim _modeFileLock;

        private const string INDEX_FILENAME = "indexes.json";
        private const string MODE_FILENAME = "mode.json";

        public PersistenceService()
        {
            _indexFileLock = new SemaphoreSlim(1, 1);
            _modeFileLock = new SemaphoreSlim(1, 1);
        }

        public async Task SaveIndexes(int bangerIndex, int fillerIndex)
        {
            await _indexFileLock.WaitAsync();

            try
            {
                var state = new IndexState
                {
                    BangerIndex = bangerIndex,
                    FillerIndex = fillerIndex
                };

                await File.WriteAllTextAsync(INDEX_FILENAME, JsonConvert.SerializeObject(state));
            }
            finally
            {
                _indexFileLock.Release();
            }
        }

        public async Task<(int, int)> GetIndexes()
        {
            await _indexFileLock.WaitAsync();

            try
            {
                if (File.Exists(INDEX_FILENAME))
                {
                    var content = await File.ReadAllTextAsync(INDEX_FILENAME);
                    var state = JsonConvert.DeserializeObject<IndexState>(content);

                    return (state.BangerIndex, state.FillerIndex);
                }

                return (-1, -1);
            }
            finally
            {
                _indexFileLock.Release();
            }
        }

        public async Task SaveMode(int currentMode)
        {
            await _modeFileLock.WaitAsync();

            try
            {
                var state = new ModeState
                {
                    Mode = currentMode
                };

                await File.WriteAllTextAsync(MODE_FILENAME, JsonConvert.SerializeObject(state));
            }
            finally
            {
                _modeFileLock.Release();
            }
        }

        public async Task<int> GetMode()
        {
            await _modeFileLock.WaitAsync();

            try
            {
                if (File.Exists(MODE_FILENAME))
                {
                    var content = await File.ReadAllTextAsync(MODE_FILENAME);
                    var state = JsonConvert.DeserializeObject<ModeState>(content);

                    return state.Mode;
                }

                return -1;
            }
            finally
            {
                _modeFileLock.Release();
            }
        }
    }

    internal class IndexState
    {
        public int BangerIndex { get; set; }
        public int FillerIndex { get; set; }
    }

    internal class ModeState
    {
        public int Mode { get; set; }
    }
}
