using Instron.Bluehill.API.BluehillAPI.Enums;
using Instron.Bluehill.API.BluehillAPI.Helpers;
using Instron.Bluehill.API.BluehillAPI.Interfaces;
using InstronBridgeSelfHost.Callbacks;
using InstronBridgeSelfHost.Models;
using System;
using System.Threading.Tasks;
using System.ServiceModel;

namespace InstronBridgeSelfHost.Services
{
    /// <summary>
    /// Serviço responsável por centralizar toda a comunicação com o Bluehill.
    /// Esta classe faz a ponte entre os controllers da API e a API oficial do Bluehill.
    /// </summary>
    public class InstronService
    {
        // Processo do Bluehill, usado para monitorar o estado do software e garantir que a conexão é válida.
        private static System.Diagnostics.Process _bluehillProcess;

        // Helper responsável por criar e gerir a conexão com o Bluehill
        private static APIConnectionHelper _connectionHelper;

        // Objeto principal usado para chamar os métodos da API do Bluehill
        private static IBluehillAPIService _bluehillApi;

        // Callback responsável por receber eventos automáticos enviados pelo Bluehill
        private static InstronCallback _callback;

        /// <summary>
        /// Indica se a API está atualmente conectada ao Bluehill.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _bluehillApi != null;
            }
        }

        /// <summary>
        /// Cria uma conexão com o Bluehill.
        /// Caso já exista uma conexão ativa, o método termina sem criar outra.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_bluehillApi != null)
                return;

            try
            {
                _connectionHelper = new APIConnectionHelper();
                _callback = new InstronCallback();

                _bluehillProcess = await _connectionHelper.LaunchBluehill();
                
                if (_bluehillProcess == null)
                    throw new Exception("Não foi possível iniciar o Bluehill.");

                await Task.Delay(5000);

                _bluehillApi = await _connectionHelper.EstablishConnection(_callback, 60);

                if (_bluehillApi == null)
                    throw new Exception("A conexão com o Bluehill não foi estabelecida.");

                InstronServiceState.IsConnected = true;
                InstronServiceState.LastState = _bluehillApi.GetCurrentState().ToString();
                InstronServiceState.LastStatusMessage = "Conectado ao Bluehill com sucesso.";
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        /// <summary>
        /// Retorna o estado atual do Bluehill.
        /// Exemplo: ReadyToStartTest, Running, Stopped, etc.
        /// </summary>
        public EnumBluehillState GetState()
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            return _bluehillApi.GetCurrentState();
        }

        /// <summary>
        /// Aguarda o Bluehill ficar pronto e inicia um novo teste.
        /// </summary>
        public async Task<EnumAPIErrors> StartTestAsync()
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            var wait = await _bluehillApi.WaitForBluehillState(
                EnumBluehillState.ReadyToStartTest,
                10
            );

            if (wait != EnumAPIErrors.NoError)
                return wait;

            return await _bluehillApi.StartTest();
        }

        /// <summary>
        /// Para o teste atualmente em execução.
        /// </summary>
        public EnumAPIErrors StopTest()
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            return _bluehillApi.StopTest();
        }

        /// <summary>
        /// Obtém os dados da tabela de resultados e estatísticas do Bluehill.
        /// </summary>
        public async Task<object[][]> GetResultsAsync(int tableNumber = 1)
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            try
            {
                return await _bluehillApi.GetResultsAndStatisticsTableData(
                    tableNumber,
                    EnumResultsAndStatisticsTable.ResultsOnly
                );
            }
            catch (System.ServiceModel.ProtocolException)
            {
                InstronServiceState.IsConnected = false;
                InstronServiceState.LastStatusMessage =
                    "Canal WCF fechado pelo Bluehill. É necessário reconectar.";

                Disconnect();

                throw new Exception("A conexão com o Bluehill foi encerrada. Faça /connect novamente e tente /results outra vez.");
            }
        }

        /// <summary>
        /// Encerra a conexão com o Bluehill e limpa os objetos utilizados.
        /// </summary>
        public void Disconnect()
        {
            if (_connectionHelper != null)
            {
                _connectionHelper.Dispose();
                _connectionHelper = null;
            }

            _bluehillApi = null;
            _callback = null;

            InstronServiceState.IsConnected = false;
            InstronServiceState.LastState = "Disconnected";
            InstronServiceState.LastStatusMessage = "Conexão encerrada.";
        }

        /// <summary>
        /// Cria uma nova amostra no Bluehill a partir de um arquivo de método.
        /// </summary>
        public async Task<EnumAPIErrors> CreateSampleAsync(string methodFilePath)
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            return await _bluehillApi.CreateSample(methodFilePath);
        }

        /// <summary>
        /// Guarda a amostra atual do Bluehill.
        /// </summary>
        public async Task<EnumAPIErrors> SaveSampleAsync(string filePath)
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            return await _bluehillApi.SaveSample(filePath);
        }

        /// <summary>
        /// Fecha a amostra atualmente aberta no Bluehill.
        /// </summary>
        public async Task<EnumAPIErrors> CloseSampleAsync()
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            return await _bluehillApi.CloseSample();
        }

        /// <summary>
        /// Obtém o valor de uma medição específica do Bluehill.
        /// Exemplo: força, deslocamento, tensão, etc.
        /// </summary>
        public async Task<double> GetMeasurementAsync(
            string measurementName,
            EnumUnits unit)
        {
            if (_bluehillApi == null)
                throw new Exception("Bluehill não está conectado.");

            return await _bluehillApi.GetMeasurementData(
                measurementName,
                unit
            );
        }
    }
}