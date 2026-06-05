using Instron.Bluehill.API.BluehillAPI.Enums;
using Instron.Bluehill.API.BluehillAPI.Interfaces;
using InstronBridgeSelfHost.InstronLogs;
using InstronBridgeSelfHost.Models;
using InstronBridgeSelfHost.Services;

namespace InstronBridgeSelfHost.Callbacks
{
    /// <summary>
    /// Classe responsável por receber notificações automáticas enviadas pelo Bluehill.
    /// 
    /// Estes métodos são chamados pelo próprio Bluehill quando:
    /// - o software é fechado;
    /// - o estado da máquina muda;
    /// - aparece uma mensagem/diálogo;
    /// - ocorre um evento no status log.
    /// </summary>
    public class InstronCallback : ICallback
    {
        // =========================================================================================
        // BLUEHILL FECHADO
        // =========================================================================================

        /// <summary>
        /// Executado automaticamente quando o Bluehill está a ser encerrado.
        /// 
        /// Importante:
        /// Quando este método é chamado, a conexão com a API deixa de ser válida.
        /// Para usar novamente, será necessário criar uma nova conexão.
        /// </summary>
        public void OnBluehillClosing()
        {
            InstronService.MarkBluehillClosedByCallback();

            Logger.Info("Bluehill foi encerrado. Estado da conexão atualizado para desconectado.");
        }


        // =========================================================================================
        // DIÁLOGOS AUTOMÁTICOS DO BLUEHILL
        // =========================================================================================

        /// <summary>
        /// Executado quando o Bluehill mostra ou está prestes a mostrar um diálogo.
        /// 
        /// Este método permite responder automaticamente a mensagens do Bluehill.
        /// Se não houver resposta adequada, o Bluehill pode ficar à espera de interação manual.
        /// </summary>
        public EnumMessageDiagResult OnBluehillDialog(
            EnumMessageID messageID,
            string prompt,
            EnumMessageDiagResult defaultResponse)
        {
            InstronServiceState.LastStatusMessage =
                $"Dialog [{messageID}]: {prompt}";

            Logger.Info(
                $"Diálogo recebido do Bluehill | ID: {messageID} | Mensagem: {prompt} | Resposta padrão: {defaultResponse}"
            );

            /*
             * Aqui tratamos mensagens conhecidas.
             * A ideia é evitar que o Bluehill fique parado à espera de uma resposta manual.
             */
            switch (messageID)
            {
                // Se algum ficheiro não existir, responde OK para confirmar a mensagem.
                case EnumMessageID.FileDoesNotExist:
                    Logger.Info("Resposta automática ao diálogo: OK");
                    return EnumMessageDiagResult.Ok;

                // Mensagens relacionadas com guardar alterações.
                // Por segurança, neste projeto escolhemos NÃO guardar automaticamente.
                case EnumMessageID.SaveSample:
                case EnumMessageID.SaveReportTemplate:
                case EnumMessageID.SaveTestParametersToMethod:
                case EnumMessageID.NewSampleWithCurrentMethod:
                case EnumMessageID.ErrorOpeningReferencedReportTemplate:
                    Logger.Info("Resposta automática ao diálogo: NO");
                    return EnumMessageDiagResult.No;

                // Para mensagens não mapeadas, usa a resposta padrão sugerida pelo Bluehill.
                default:
                    Logger.Info("Resposta automática ao diálogo: resposta padrão do Bluehill");
                    return defaultResponse;
            }
        }


        // =========================================================================================
        // ALTERAÇÃO DE ESTADO DO BLUEHILL
        // =========================================================================================

        /// <summary>
        /// Executado sempre que o estado do Bluehill muda.
        /// 
        /// Exemplos de estados:
        /// - BluehillHome
        /// - SampleOpened
        /// - ReadyToStartTest
        /// - Running
        /// - Calculating
        /// </summary>
        public void OnStateChanged(EnumBluehillState state)
        {
            InstronServiceState.LastState = state.ToString();

            Logger.Info("Estado do Bluehill alterado para: " + state);
        }


        // =========================================================================================
        // EVENTOS DE STATUS / LOG DO BLUEHILL
        // =========================================================================================

        /// <summary>
        /// Executado quando o Bluehill gera uma mensagem no Status Log.
        /// 
        /// O retorno é importante:
        /// - true  = indica que a mensagem foi tratada e pode ser fechada automaticamente;
        /// - false = permite que o Bluehill mostre a mensagem na interface.
        /// </summary>
        public bool OnStatusLogEvent(int statusCode, string statusMessage)
        {
            InstronServiceState.LastStatusCode = statusCode;
            InstronServiceState.LastStatusMessage = statusMessage;

            Logger.Info(
                $"Status Log recebido do Bluehill | Código: {statusCode} | Mensagem: {statusMessage}"
            );

            /*
             * Retornamos true para informar ao Bluehill que o evento foi tratado.
             * Isso evita que a aplicação fique bloqueada à espera do utilizador fechar o status log.
             */
            return true;
        }
    }
}
