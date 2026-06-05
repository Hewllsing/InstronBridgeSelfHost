using Instron.Bluehill.API.BluehillAPI.Enums;
using Instron.Bluehill.API.BluehillAPI.Helpers;
using Instron.Bluehill.API.BluehillAPI.Interfaces;
using InstronBridgeSelfHost.Callbacks;
using InstronBridgeSelfHost.InstronLogs;
using InstronBridgeSelfHost.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace InstronBridgeSelfHost.Services
{
    /// <summary>
    /// Centraliza a comunicacao com o Bluehill Universal.
    /// A classe tambem protege a API contra conexoes WCF antigas quando o Bluehill fecha ou reinicia.
    /// </summary>
    public class InstronService
    {
        // Evita duas requests tentando abrir/conectar o Bluehill ao mesmo tempo.
        private static readonly SemaphoreSlim ConnectionLock = new SemaphoreSlim(1, 1);

        // O nome do processo pode variar por versao, por isso usamos busca parcial.
        private static readonly string[] BluehillProcessNameParts =
        {
            "bluehill"
        };

        // Processo do Bluehill associado a conexao atual, quando conhecido.
        private static Process _bluehillProcess;

        // Helper oficial da API Instron/Bluehill para criar e encerrar a conexao WCF.
        private static APIConnectionHelper _connectionHelper;

        // Proxy WCF principal exposto pela Bluehill.API.dll.
        private static IBluehillAPIService _bluehillApi;

        // Callback usado pelo Bluehill para avisar estados, dialogs e encerramento.
        private static InstronCallback _callback;

        /// <summary>
        /// Indica se existe uma conexao utilizavel neste momento.
        /// Esta propriedade valida o processo real do Bluehill, nao apenas se o proxy WCF existe.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                RefreshConnectionState();
                return _bluehillApi != null;
            }
        }

        /// <summary>
        /// Atualiza o estado em memoria sem abrir o Bluehill.
        /// Usado pelo health check para refletir se o processo foi fechado manualmente.
        /// </summary>
        public void RefreshConnectionState()
        {
            if (_bluehillApi == null)
            {
                InstronServiceState.IsConnected = false;
                return;
            }

            if (!IsBluehillProcessRunning())
            {
                ResetConnection(false);
                InstronServiceState.LastState = "BluehillClosed";
                InstronServiceState.LastStatusMessage = "Bluehill nao esta em execucao.";
                return;
            }

            try
            {
                InstronServiceState.LastState = _bluehillApi.GetCurrentState().ToString();
                InstronServiceState.IsConnected = true;
                InstronServiceState.LastError = null;
            }
            catch (Exception ex)
            {
                if (!IsConnectionException(ex))
                {
                    Logger.Error("Erro inesperado ao validar a conexao com o Bluehill.", ex);
                    InstronServiceState.LastError = ex.Message;
                    return;
                }

                ResetConnection(false);
                InstronServiceState.LastState = "Disconnected";
                InstronServiceState.LastStatusMessage = "Conexao WCF invalida. A proxima consulta ira reconectar.";
                InstronServiceState.LastError = ex.Message;

                Logger.Error("Conexao WCF invalida ao validar estado do Bluehill.", ex);
            }
        }

        /// <summary>
        /// Chamado pelo callback quando o Bluehill avisa que esta fechando.
        /// A limpeza nao mata processo aqui; apenas descarta o proxy antigo.
        /// </summary>
        public static void MarkBluehillClosedByCallback()
        {
            ResetConnection(false);
            InstronServiceState.IsConnected = false;
            InstronServiceState.LastState = "BluehillClosed";
            InstronServiceState.LastStatusMessage = "Bluehill foi encerrado.";
        }

        /// <summary>
        /// Garante uma sessao ativa com o Bluehill.
        /// Primeiro tenta ligar a uma instancia ja aberta; se nao existir, abre o Bluehill pela API.
        /// </summary>
        public async Task ConnectAsync()
        {
            await ConnectionLock.WaitAsync();

            try
            {
                RefreshConnectionState();

                if (_bluehillApi != null)
                    return;

                ResetConnection(false);

                _connectionHelper = new APIConnectionHelper();
                _callback = new InstronCallback();

                try
                {
                    // 1) Tenta aproveitar um Bluehill aberto manualmente pelo operador.
                    _bluehillApi = await _connectionHelper.EstablishConnection(_callback, 10);
                    _bluehillProcess = FindBluehillProcess();

                    Logger.Info("Conectado a uma instancia existente do Bluehill.");
                }
                catch (Exception existingConnectionError)
                {
                    Logger.Info("Nao foi possivel conectar a uma instancia existente do Bluehill: " + existingConnectionError.Message);

                    try
                    {
                        if (IsBluehillProcessRunning())
                        {
                            // Se existe processo mas o endpoint WCF nao responde, tratamos como sessao presa.
                            Logger.Info("Processo Bluehill encontrado sem conexao valida. A encerrar antes de iniciar nova sessao.");
                            CloseBluehillProcess();
                        }

                        // 2) Se nao houver sessao aberta, inicia o Bluehill via helper oficial.
                        _bluehillProcess = await _connectionHelper.LaunchBluehill();

                        if (_bluehillProcess == null)
                            throw new Exception("Nao foi possivel iniciar o Bluehill.");

                        Logger.Info("Bluehill iniciado pela API. PID: " + _bluehillProcess.Id);

                        // O Bluehill demora a publicar o endpoint WCF depois de abrir a UI.
                        await Task.Delay(5000);

                        _bluehillApi = await _connectionHelper.EstablishConnection(_callback, 60);
                    }
                    catch (Exception launchError)
                    {
                        ResetConnection(false);

                        InstronServiceState.IsConnected = false;
                        InstronServiceState.LastStatusMessage = launchError.Message;
                        InstronServiceState.LastError = launchError.Message;

                        Logger.Error("Erro ao iniciar/conectar ao Bluehill.", launchError);

                        throw;
                    }
                }

                if (_bluehillApi == null)
                    throw new Exception("A conexao com o Bluehill nao foi estabelecida.");

                InstronServiceState.IsConnected = true;
                InstronServiceState.LastState = _bluehillApi.GetCurrentState().ToString();
                InstronServiceState.LastStatusMessage = "Conectado ao Bluehill com sucesso.";
                InstronServiceState.LastError = null;
            }
            finally
            {
                ConnectionLock.Release();
            }
        }

        /// <summary>
        /// Retorna o estado atual. Se o Bluehill nao estiver conectado, conecta automaticamente.
        /// </summary>
        public async Task<EnumBluehillState> GetStateAsync()
        {
            return await ExecuteWithReconnectAsync(
                () => Task.FromResult(_bluehillApi.GetCurrentState()),
                "obter estado"
            );
        }

        /// <summary>
        /// Compatibilidade com chamadas antigas do controller.
        /// </summary>
        public EnumBluehillState GetState()
        {
            return GetStateAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Inicia um teste. Mantido por compatibilidade, mas nao deve ser exposto ao frontend operacional.
        /// </summary>
        public async Task<EnumAPIErrors> StartTestAsync()
        {
            return await ExecuteWithReconnectAsync(async () =>
            {
                var wait = await _bluehillApi.WaitForBluehillState(
                    EnumBluehillState.ReadyToStartTest,
                    10
                );

                if (wait != EnumAPIErrors.NoError)
                    return wait;

                return await _bluehillApi.StartTest();
            }, "iniciar teste");
        }

        /// <summary>
        /// Para o teste atual. Mantido por compatibilidade, mas nao deve ser exposto ao frontend operacional.
        /// </summary>
        public async Task<EnumAPIErrors> StopTestAsync()
        {
            return await ExecuteWithReconnectAsync(
                () => Task.FromResult(_bluehillApi.StopTest()),
                "parar teste"
            );
        }

        /// <summary>
        /// Compatibilidade com chamadas antigas do controller.
        /// </summary>
        public EnumAPIErrors StopTest()
        {
            return StopTestAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Obtem a tabela de resultados do Bluehill, conectando automaticamente se necessario.
        /// </summary>
        public async Task<object[][]> GetResultsAsync(int tableNumber = 1)
        {
            return await ExecuteWithReconnectAsync(async () =>
            {
                return await _bluehillApi.GetResultsAndStatisticsTableData(
                    tableNumber,
                    EnumResultsAndStatisticsTable.ResultsOnly
                );
            }, "obter resultados");
        }

        /// <summary>
        /// Obtem resultados e converte a matriz do Bluehill em objeto com headers e rows.
        /// </summary>
        public async Task<object> GetFormattedResultsAsync(int tableNumber = 1)
        {
            var data = await GetResultsAsync(tableNumber);

            if (data == null || data.Length == 0)
            {
                return new
                {
                    tableNumber = tableNumber,
                    headers = new string[] { },
                    rows = new object[] { }
                };
            }

            var headers = data[0]
                .Select(h => h == null ? "" : h.ToString())
                .ToArray();

            var rows = new List<Dictionary<string, object>>();

            for (int i = 1; i < data.Length; i++)
            {
                var row = new Dictionary<string, object>();

                for (int j = 0; j < headers.Length; j++)
                {
                    var key = string.IsNullOrWhiteSpace(headers[j])
                        ? "Column" + (j + 1)
                        : headers[j];

                    var value = j < data[i].Length ? data[i][j] : null;

                    row[key] = value;
                }

                rows.Add(row);
            }

            return new
            {
                tableNumber = tableNumber,
                headers = headers,
                rows = rows
            };
        }

        /// <summary>
        /// Encerra a conexao WCF e tenta fechar o processo Bluehill.
        /// Se a janela nao responder, finaliza o processo para evitar sessoes presas no Task Manager.
        /// </summary>
        public void Disconnect()
        {
            ResetConnection(true);

            InstronServiceState.IsConnected = false;
            InstronServiceState.LastState = "Disconnected";
            InstronServiceState.LastStatusMessage = "Conexao encerrada e processo Bluehill finalizado.";
        }

        /// <summary>
        /// Cria uma amostra. Mantido por compatibilidade com a API existente.
        /// </summary>
        public async Task<EnumAPIErrors> CreateSampleAsync(string methodFilePath)
        {
            return await ExecuteWithReconnectAsync(
                () => _bluehillApi.CreateSample(methodFilePath),
                "criar amostra"
            );
        }

        /// <summary>
        /// Guarda a amostra atual. Mantido por compatibilidade com a API existente.
        /// </summary>
        public async Task<EnumAPIErrors> SaveSampleAsync(string filePath)
        {
            return await ExecuteWithReconnectAsync(
                () => _bluehillApi.SaveSample(filePath),
                "guardar amostra"
            );
        }

        /// <summary>
        /// Fecha a amostra atual. Mantido por compatibilidade com a API existente.
        /// </summary>
        public async Task<EnumAPIErrors> CloseSampleAsync()
        {
            return await ExecuteWithReconnectAsync(
                () => _bluehillApi.CloseSample(),
                "fechar amostra"
            );
        }

        /// <summary>
        /// Obtem uma medicao especifica do Bluehill, conectando automaticamente se necessario.
        /// </summary>
        public async Task<double> GetMeasurementAsync(
            string measurementName,
            EnumUnits unit)
        {
            return await ExecuteWithReconnectAsync(
                () => _bluehillApi.GetMeasurementData(
                    measurementName,
                    unit
                ),
                "obter medicao"
            );
        }

        /// <summary>
        /// Conecta automaticamente quando a request precisa do Bluehill.
        /// </summary>
        private async Task EnsureConnectedAsync()
        {
            RefreshConnectionState();

            if (_bluehillApi != null)
                return;

            await ConnectAsync();
        }

        /// <summary>
        /// Executa uma chamada Bluehill e tenta reconectar uma vez caso o canal WCF esteja invalido.
        /// </summary>
        private async Task<T> ExecuteWithReconnectAsync<T>(
            Func<Task<T>> action,
            string actionName)
        {
            await EnsureConnectedAsync();

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                if (!IsConnectionException(ex))
                    throw;

                Logger.Error("Conexao Bluehill invalida ao " + actionName + ". A tentar reconectar.", ex);

                ResetConnection(false);
                await EnsureConnectedAsync();

                return await action();
            }
        }

        /// <summary>
        /// Confirma se existe processo Bluehill ativo.
        /// </summary>
        private static bool IsBluehillProcessRunning()
        {
            return GetCurrentBluehillProcess() != null;
        }

        /// <summary>
        /// Usa o processo guardado, se ainda existir; caso contrario procura outro processo Bluehill.
        /// </summary>
        private static Process GetCurrentBluehillProcess()
        {
            try
            {
                if (_bluehillProcess != null)
                {
                    _bluehillProcess.Refresh();

                    if (!_bluehillProcess.HasExited)
                        return _bluehillProcess;
                }
            }
            catch
            {
                _bluehillProcess = null;
            }

            _bluehillProcess = FindBluehillProcess();

            return _bluehillProcess;
        }

        /// <summary>
        /// Procura processos cujo nome contenha "bluehill".
        /// </summary>
        private static Process FindBluehillProcess()
        {
            return Process.GetProcesses()
                .FirstOrDefault(IsBluehillProcess);
        }

        /// <summary>
        /// Evita excecoes de permissao ao ler processos de sistema.
        /// </summary>
        private static bool IsBluehillProcess(Process process)
        {
            try
            {
                return BluehillProcessNameParts.Any(part =>
                    process.ProcessName.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0
                );
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lista de erros que normalmente indicam canal WCF fechado/stale.
        /// </summary>
        private static bool IsConnectionException(Exception ex)
        {
            return ex is CommunicationException
                || ex is ProtocolException
                || ex is ObjectDisposedException
                || ex is TimeoutException
                || ex is InvalidOperationException;
        }

        /// <summary>
        /// Limpa objetos WCF. Opcionalmente tambem encerra o processo Bluehill.
        /// </summary>
        private static void ResetConnection(bool closeBluehill)
        {
            if (_connectionHelper != null)
            {
                _connectionHelper.Dispose();
                _connectionHelper = null;
            }

            _bluehillApi = null;
            _callback = null;

            if (closeBluehill)
                CloseBluehillProcess();

            InstronServiceState.IsConnected = false;
        }

        /// <summary>
        /// Fecha o Bluehill de forma graciosa e, se ficar preso, mata o processo.
        /// </summary>
        private static void CloseBluehillProcess()
        {
            var processes = Process.GetProcesses()
                .Where(IsBluehillProcess)
                .ToList();

            foreach (var process in processes)
            {
                try
                {
                    process.Refresh();

                    if (process.HasExited)
                        continue;

                    Logger.Info("A encerrar processo Bluehill: " + process.ProcessName + " (" + process.Id + ")");

                    if (process.CloseMainWindow() && process.WaitForExit(10000))
                        continue;

                    Logger.Info("Processo Bluehill nao encerrou pela janela. A finalizar processo: " + process.Id);

                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Logger.Error("Erro ao encerrar processo Bluehill.", ex);
                }
            }

            _bluehillProcess = null;
        }
    }
}
