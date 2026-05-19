using InstronBridgeAPI;
using Owin;
using System.Web.Http;

namespace InstronBridgeSelfHost
{
    /// <summary>
    /// Classe responsável por configurar o pipeline OWIN
    /// e iniciar a Web API self-host.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Método chamado automaticamente quando o servidor OWIN inicia.
        /// Aqui configuramos as rotas e registramos a Web API.
        /// </summary>
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            // Registra as configurações da Web API
            WebApiConfig.Register(config);

            // Liga a Web API ao pipeline OWIN
            app.UseWebApi(config);
        }
    }
}