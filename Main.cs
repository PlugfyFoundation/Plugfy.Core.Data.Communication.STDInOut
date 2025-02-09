using Newtonsoft.Json;

using Plugfy.Core.Commons.Communication;

using System.Text;

namespace Plugfy.Core.Communication
{
    public class STDInOut : IDataCommunication
    {
        public string Name => "STDInOut";
        public string Description => "Comunicação utilizando entrada e saída padrão.";

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        private StreamReader _stdin;
        private StreamWriter _stdout;
        private bool _isClosed;

        public bool IsClosed => _isClosed;

        public async Task InitializeAsync(dynamic parameters)
        {
            _stdin = new StreamReader(Console.OpenStandardInput(), Encoding.Default);
            _stdout = new StreamWriter(Console.OpenStandardOutput(), Encoding.Default)
            {
                AutoFlush = true 
            };

            Console.SetOut(_stdout);
            Console.SetError(_stdout); 

            _isClosed = false;
            await Task.CompletedTask;
        }

        public async Task SendDataAsync(dynamic data)
        {
            if (_stdout == null)
                throw new InvalidOperationException("STDOUT não está inicializado.");

            try
            {
                lock (_stdout)  
                {
                    _stdout.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(data));
                    _stdout.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao enviar dados para STDOUT: {ex}");
            }

            await Task.CompletedTask;
        }

        public async Task CloseAsync()
        {
            _isClosed = true;
            _stdout?.Flush();
            _stdout?.Dispose();
            _stdin?.Dispose();
            await Task.CompletedTask;
        }

        public async Task StartListeningAsync()
        {
            if (_stdin == null)
                throw new InvalidOperationException("STDIN não está inicializado.");

            string line;
            while (!_isClosed && (line = await _stdin.ReadLineAsync()) != null)
            {
                try
                {
                    var parsedData = JsonConvert.DeserializeObject<DataReceivedEventArgs>(line);
                    DataReceived?.Invoke(this, parsedData);
                }
                catch (JsonReaderException ex)
                {
                    Console.Error.WriteLine($"Erro ao processar entrada JSON: {ex}");
                }
            }
        }
    }
}
