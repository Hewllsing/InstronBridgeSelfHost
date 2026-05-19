using System.Web.Http;

namespace InstronBridgeAPI
{
    /// <summary>
    /// Classe responsável pela configuração global da Web API.
    /// Aqui são definidas as rotas e configurações principais da aplicação.
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Método executado no arranque da aplicação
        /// para registar todas as configurações da Web API.
        /// </summary>
        /// <param name="config">
        /// Objeto de configuração da Web API.
        /// </param>
        public static void Register(HttpConfiguration config)
        {

            // =====================================================================================
            // ATIVA ROTAS POR ATRIBUTOS
            // =====================================================================================

            /*
             * Permite utilizar atributos como:
             * [Route("api/exemplo")]
             * diretamente nos controllers.
             */
            config.MapHttpAttributeRoutes();


            // =====================================================================================
            // ROTA PADRÃO DA API
            // =====================================================================================

            /*
             * Define a rota padrão da aplicação.
             * Exemplo:
             * api/Instron
             * api/Instron/1
             */
            config.Routes.MapHttpRoute(
                name: "DefaultApi",

                // Estrutura padrão das URLs da API
                routeTemplate: "api/{controller}/{id}",

                // O parâmetro "id" é opcional
                defaults: new
                {
                    id = RouteParameter.Optional
                }
            );


            // =====================================================================================
            // FORMATAÇÃO DO JSON
            // =====================================================================================

            /*
             * Configura o retorno JSON da API para ficar formatado
             * de forma mais legível (indentado).
             *
             * Sem isso:
             * {"nome":"Leonardo","idade":22}
             *
             * Com isso:
             * {
             *    "nome": "Leonardo",
             *    "idade": 22
             * }
             */
            config.Formatters.JsonFormatter.SerializerSettings.Formatting =
                Newtonsoft.Json.Formatting.Indented;
        }
    }
}