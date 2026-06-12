using Microsoft.Owin.Hosting;
using System;
using InstronBridgeSelfHost.InstronLogs;

namespace InstronBridgeSelfHost
{
    /// <summary>
    /// Classe principal responsável por iniciar o servidor self-host.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            // Por padrao a API fica acessivel pela rede, nao apenas por localhost.
            // Pode ser sobrescrito pelo primeiro argumento ou pela variavel INSTRON_BRIDGE_URL.
            string baseAddress = ResolveBaseAddress(args);

            try
            {
                Logger.Info("Iniciando Instron Bridge SelfHost em " + baseAddress);

                // Inicia o servidor OWIN e mantem a sessao com o Bluehill viva enquanto a consola estiver aberta.
                using (WebApp.Start<Startup>(url: baseAddress))
                {
                    PrintStartupInformation(baseAddress);

                    // Mantem o programa aberto.
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Falha ao iniciar Instron Bridge SelfHost.", ex);
                PrintStartupError(ex, baseAddress);
            }
        }

        private static string ResolveBaseAddress(string[] args)
        {
            string configuredUrl = null;

            if (args != null && args.Length > 0)
            {
                configuredUrl = args[0];
            }

            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                configuredUrl = Environment.GetEnvironmentVariable("INSTRON_BRIDGE_URL");
            }

            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                configuredUrl = "http://+:9000/";
            }

            configuredUrl = configuredUrl.Trim();

            if (!configuredUrl.EndsWith("/"))
            {
                configuredUrl += "/";
            }

            return configuredUrl;
        }

        private static void PrintStartupInformation(string baseAddress)
        {
            string localBaseAddress = ToLocalDisplayAddress(baseAddress);
            string networkBaseAddress = ToNetworkDisplayAddress(baseAddress);

            Console.WriteLine("==========================================");
            Console.WriteLine(" Instron Bridge API Self Host iniciado");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("Binding OWIN:");
            Console.WriteLine(baseAddress);
            Console.WriteLine();
            Console.WriteLine("Teste local:");
            Console.WriteLine(localBaseAddress + "api/instron/health");
            Console.WriteLine();
            Console.WriteLine("Teste pela rede:");
            Console.WriteLine(networkBaseAddress + "api/instron/health");
            Console.WriteLine();
            Console.WriteLine("Endpoints principais:");
            Console.WriteLine(localBaseAddress + "api/instron/health");
            Console.WriteLine(localBaseAddress + "api/instron/connect");
            Console.WriteLine(localBaseAddress + "api/instron/state");
            Console.WriteLine(localBaseAddress + "api/instron/results?tableNumber=1");
            Console.WriteLine(localBaseAddress + "api/instron/results/formatted?tableNumber=1");
            Console.WriteLine();
            Console.WriteLine("Pressione ENTER para encerrar...");
            Console.WriteLine();
        }

        private static string ToLocalDisplayAddress(string baseAddress)
        {
            return baseAddress
                .Replace("://+:", "://localhost:")
                .Replace("://0.0.0.0:", "://localhost:");
        }

        private static string ToNetworkDisplayAddress(string baseAddress)
        {
            return baseAddress
                .Replace("://+:", "://IP_DA_MAQUINA_INSTRON:")
                .Replace("://0.0.0.0:", "://IP_DA_MAQUINA_INSTRON:")
                .Replace("://localhost:", "://IP_DA_MAQUINA_INSTRON:")
                .Replace("://127.0.0.1:", "://IP_DA_MAQUINA_INSTRON:");
        }

        private static void PrintStartupError(Exception ex, string baseAddress)
        {
            Console.WriteLine("==========================================");
            Console.WriteLine(" Falha ao iniciar Instron Bridge API");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("Binding tentado:");
            Console.WriteLine(baseAddress);
            Console.WriteLine();
            Console.WriteLine("Erro:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("Se estiver usando http://+:9000/, execute como administrador na maquina Instron:");
            Console.WriteLine("netsh http add urlacl url=http://+:9000/ user=Everyone");
            Console.WriteLine("New-NetFirewallRule -DisplayName \"Instron Bridge API 9000\" -Direction Inbound -Protocol TCP -LocalPort 9000 -Action Allow");
            Console.WriteLine();
            Console.WriteLine("Pressione ENTER para encerrar...");
            Console.ReadLine();
        }
    }
}
