namespace InstronBridgeSelfHost.Models
{
    public static class InstronServiceState
    {
        // Indica se a API está conectada ao Bluehill
        public static bool IsConnected { get; set; }

        // Último estado recebido do Bluehill
        public static string LastState { get; set; }

        // Último código de status recebido
        public static int? LastStatusCode { get; set; }

        // Última mensagem de status recebida
        public static string LastStatusMessage { get; set; }

        // Último erro interno
        public static string LastError { get; set; }

        // Limpa o estado atual
        public static void Clear()
        {
            IsConnected = false;
            LastState = null;
            LastStatusCode = null;
            LastStatusMessage = null;
            LastError = null;
        }
    }
}