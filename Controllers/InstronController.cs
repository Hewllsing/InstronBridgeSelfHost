using Instron.Bluehill.API.BluehillAPI.Enums;
using Instron.Bluehill.API.BluehillAPI.Helpers;
using Instron.Bluehill.API.BluehillAPI.Interfaces;
using InstronBridgeSelfHost.Callbacks;
using InstronBridgeSelfHost.InstronLogs;
using InstronBridgeSelfHost.Models;
using InstronBridgeSelfHost.Services;
using InstronBridgeSelfHost.Testes;
using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstronBridgeSelfHost.Controllers
{
    [RoutePrefix("api/instron")]
    public class InstronController : ApiController
    {
        /// <summary>
        /// Controller responsável por disponibilizar endpoints da API
        /// para comunicação e controlo do software Bluehill.
        /// </summary>

        // Instância única do serviço principal da aplicação
        // Responsável pela conexão e operações com o Bluehill
        private static readonly InstronService _service = new InstronService();


        // =========================================================================================
        // ENDPOINT DE VERIFICAÇÃO DA API
        // =========================================================================================

        /// <summary>
        /// Verifica se a API está online e retorna o estado atual da conexão com o Bluehill.
        /// </summary>
        [HttpGet]
        [Route("health")]
        public IHttpActionResult Health()
        {
            _service.RefreshConnectionState();

            return Ok(new
            {
                status = "online",
                connected = _service.IsConnected,
                lastState = InstronServiceState.LastState,
                lastStatusCode = InstronServiceState.LastStatusCode,
                lastStatusMessage = InstronServiceState.LastStatusMessage
            });
        }


        // =========================================================================================
        // CONEXÃO COM O BLUEHILL
        // =========================================================================================

        /// <summary>
        /// Realiza a conexão da API com o software Bluehill.
        /// </summary>
        [HttpPost]
        [Route("connect")]
        public async Task<IHttpActionResult> Connect()
        {
            try
            {   
                await _service.ConnectAsync();

                return Ok(new { message = "Conectado ao Bluehill com sucesso." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // OBTÉM O ESTADO ATUAL DO BLUEHILL
        // =========================================================================================

        /// <summary>
        /// Retorna o estado atual do Bluehill.
        /// Exemplo: Idle, Running, Stopped, etc.
        /// </summary>
        [HttpGet]
        [Route("state")]
        public async Task<IHttpActionResult> State()
        {
            try
            {
                var state = await _service.GetStateAsync();

                return Ok(new
                {
                    state = state.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // INICIAR TESTE
        // =========================================================================================

        /// <summary>
        /// Inicia um teste no Bluehill.
        /// </summary>
        [HttpPost]
        [Route("start-test")]
        public async Task<IHttpActionResult> StartTest()
        {
            try
            {
                // Verifica conexão antes de iniciar o teste
                if (!_service.IsConnected)
                    return BadRequest("Bluehill não está conectado.");

                var result = await _service.StartTestAsync();

                // Caso a API do Bluehill retorne erro
                if (result != EnumAPIErrors.NoError)
                    return BadRequest("Erro ao iniciar teste: " + result);

                return Ok(new
                {
                    message = "Teste iniciado com sucesso.",
                    result = result.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // PARAR TESTE
        // =========================================================================================

        /// <summary>
        /// Interrompe o teste atualmente em execução.
        /// </summary>
        [HttpPost]
        [Route("stop-test")]
        public IHttpActionResult StopTest()
        {
            try
            {
                // Verifica conexão antes de parar o teste
                if (!_service.IsConnected)
                    return BadRequest("Bluehill não está conectado.");

                var result = _service.StopTest();

                // Verifica se ocorreu erro na operação
                if (result != EnumAPIErrors.NoError)
                    return BadRequest("Erro ao parar teste: " + result);

                return Ok(new
                {
                    message = "Teste parado com sucesso.",
                    result = result.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // OBTER RESULTADOS
        // =========================================================================================

        /// <summary>
        /// Obtém os dados da tabela de resultados e estatísticas do teste.
        /// </summary>
        [HttpGet]
        [Route("results")]
        public async Task<IHttpActionResult> Results(int tableNumber = 1)
        {
            try
            {
                var data = await _service.GetResultsAsync(tableNumber);

                return Ok(new
                {
                    tableNumber,
                    data
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpGet]
        [Route("results/formatted")]
        public async Task<IHttpActionResult> ResultsFormatted(int tableNumber = 1)
        {
            try
            {
                var result = await _service.GetFormattedResultsAsync(tableNumber);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // DESCONECTAR DO BLUEHILL
        // =========================================================================================

        /// <summary>
        /// Finaliza a conexão com o Bluehill
        /// e limpa os estados armazenados da aplicação.
        /// </summary>
        [HttpPost]
        [Route("disconnect")]
        public IHttpActionResult Disconnect()
        {
            _service.Disconnect();

            return Ok(new
            {
                message = InstronServiceState.LastStatusMessage
            });
        }


        // =========================================================================================
        // CRIAR NOVA AMOSTRA
        // =========================================================================================

        /// <summary>
        /// Cria uma nova amostra utilizando um arquivo de método do Bluehill.
        /// </summary>
        [HttpPost]
        [Route("create-sample")]
        public async Task<IHttpActionResult> CreateSample([FromBody] CreateSampleRequest request)
        {
            try
            {
                // Verifica conexão ativa
                if (!_service.IsConnected)
                    return BadRequest("Bluehill não está conectado.");

                // Valida dados recebidos
                if (request == null || string.IsNullOrWhiteSpace(request.MethodFilePath))
                    return BadRequest("Informe o campo MethodFilePath.");

                var result = await _service.CreateSampleAsync(request.MethodFilePath);

                // Verifica possíveis erros da API
                if (result != EnumAPIErrors.NoError)
                    return BadRequest("Erro ao criar amostra: " + result);

                return Ok(new
                {
                    message = "Amostra criada com sucesso.",
                    result = result.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // GUARDAR AMOSTRA
        // =========================================================================================

        /// <summary>
        /// Guarda a amostra atual.
        /// Pode receber opcionalmente um caminho personalizado para salvar o arquivo.
        /// </summary>
        [HttpPost]
        [Route("save-sample")]
        public async Task<IHttpActionResult> SaveSample([FromBody] SaveSampleRequest request)
        {
            try
            {
                // Verifica conexão ativa
                if (!_service.IsConnected)
                    return BadRequest("Bluehill não está conectado.");

                // Caso não venha caminho, será utilizado o padrão do Bluehill
                var filePath = request == null ? null : request.FilePath;

                var result = await _service.SaveSampleAsync(filePath);

                // Verifica se ocorreu erro ao guardar
                if (result != EnumAPIErrors.NoError)
                    return BadRequest("Erro ao guardar amostra: " + result);

                return Ok(new
                {
                    message = "Amostra guardada com sucesso.",
                    result = result.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // FECHAR AMOSTRA
        // =========================================================================================

        /// <summary>
        /// Fecha a amostra atualmente aberta no Bluehill.
        /// </summary>
        [HttpPost]
        [Route("close-sample")]
        public async Task<IHttpActionResult> CloseSample()
        {
            try
            {
                // Verifica conexão ativa
                if (!_service.IsConnected)
                    return BadRequest("Bluehill não está conectado.");

                var result = await _service.CloseSampleAsync();

                // Verifica erro retornado pela API
                if (result != EnumAPIErrors.NoError)
                    return BadRequest("Erro ao fechar amostra: " + result);

                return Ok(new
                {
                    message = "Amostra fechada com sucesso.",
                    result = result.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // OBTER MEDIÇÕES
        // =========================================================================================

        /// <summary>
        /// Obtém uma medição específica do Bluehill.
        /// Exemplo: força, deslocamento, tensão, etc.
        /// </summary>
        [HttpGet]
        [Route("measurement")]
        public async Task<IHttpActionResult> Measurement(
            string measurementName,
            string unit)
        {
            try
            {
                EnumUnits parsedUnit;

                // Converte a unidade recebida em texto para Enum
                if (!Enum.TryParse(unit, true, out parsedUnit))
                    return BadRequest("Unidade inválida.");

                var value = await _service.GetMeasurementAsync(
                    measurementName,
                    parsedUnit
                );

                return Ok(new
                {
                    measurement = measurementName,
                    unit = parsedUnit.ToString(),
                    value = value
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        // =========================================================================================
        // ENDPOINT DE TESTE
        // =========================================================================================

        /// <summary>
        /// Endpoint simples utilizado para testar envio de dados do frontend.
        /// </summary>
        [HttpPost]
        [Route("teste")]
        public IHttpActionResult Teste(PedidoBluehillDto pedido)
        {
            return Ok(new
            {
                mensagem = "Dados recebidos com sucesso",
                dados = pedido
            });
        }


        // =========================================================================================
        // TESTE DE LOGS
        // =========================================================================================

        /// <summary>
        /// Endpoint utilizado para testar o sistema de logs da aplicação.
        /// Regista informações recebidas no ficheiro de logs.
        /// </summary>
        [HttpPost]
        [Route("testeLogs")]
        public IHttpActionResult TesteLogs(PedidoBluehillDto pedido)
        {
            try
            {
                // Regista entrada no endpoint
                Logger.Info("Entrou no endpoint testeLogs");

                // Valida se o JSON foi recebido corretamente
                if (pedido == null)
                {
                    Logger.Error("Pedido veio null");

                    return BadRequest("JSON inválido.");
                }

                // Regista os dados recebidos no ficheiro de logs
                Logger.Info("Nome recebido: " + pedido.Nome);
                Logger.Info("NIF recebido: " + pedido.Nif);
                Logger.Info("Email recebido: " + pedido.Email);

                return Ok(new
                {
                    sucesso = true,
                    mensagem = "Log criado com sucesso"
                });
            }
            catch (Exception ex)
            {
                // Regista erro completo no ficheiro de logs
                Logger.Error("Erro no endpoint testeLogs", ex);

                return InternalServerError(ex);
            }
        }
    }
}
