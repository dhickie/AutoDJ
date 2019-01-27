using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoDJ.Services
{
    public class PersistenceService : IPersistenceService
    {
        private readonly SemaphoreSlim _fileLock;

        private const string FILENAME = "indexes.json";

        public PersistenceService()
        {
            _fileLock = new SemaphoreSlim(1, 1);
        }

        public async Task SaveIndexes(int bangerIndex, int fillerIndex)
        {
            await _fileLock.WaitAsync();

            try
            {
                var state = new IndexState
                {
                    BangerIndex = bangerIndex,
                    FillerIndex = fillerIndex
                };

                await File.WriteAllTextAsync(FILENAME, JsonConvert.SerializeObject(state));
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<(int, int)> GetIndexes()
        {
            await _fileLock.WaitAsync();

            try
            {
                if (File.Exists(FILENAME))
                {
                    var content = await File.ReadAllTextAsync(FILENAME);
                    var state = JsonConvert.DeserializeObject<IndexState>(content);

                    return (state.BangerIndex, state.FillerIndex);
                }

                return (-1, -1);
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }

    internal class IndexState
    {
        public int BangerIndex { get; set; }
        public int FillerIndex { get; set; }
    }
}
