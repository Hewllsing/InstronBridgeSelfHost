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
            // URL onde a API ficará disponível
            string baseAddress = "http://localhost:9000/";

            // Inicia o servidor OWIN
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("==========================================");
                Console.WriteLine(" Instron Bridge API Self Host iniciado");
                Console.WriteLine("==========================================");
                Console.WriteLine();
                Console.WriteLine("URL:");
                Console.WriteLine(baseAddress);
                Console.WriteLine();
                Console.WriteLine("Endpoints:");
                Console.WriteLine(baseAddress + "api/instron/health");
                Console.WriteLine(baseAddress + "api/instron/connect");
                Console.WriteLine(baseAddress + "api/instron/state");
                Console.WriteLine(baseAddress + "api/instron/results?tableNumber=1");
                Console.WriteLine(baseAddress + "api/instron/results/formatted?tableNumber=1");
                Console.WriteLine();
                Console.WriteLine("Pressione ENTER para encerrar...");
                Console.WriteLine();

                // Mantém o programa aberto
                Console.ReadLine();
            }
        }
    }
}