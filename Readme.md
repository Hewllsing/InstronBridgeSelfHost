# Instron Bridge SelfHost API

API local desenvolvida em C#/.NET Framework para fazer a ponte entre uma aplicação externa e o software **Bluehill Universal** da Instron.

A aplicação roda como um `.exe` SelfHost, sem depender do IIS, mantendo a conexão com o Bluehill ativa enquanto o programa estiver aberto.

---

## 1. Objetivo do projeto

O objetivo deste projeto é permitir que uma aplicação externa, como um sistema em PHP, consiga comunicar com o Bluehill Universal através de endpoints HTTP.

A API permite:

- verificar se a aplicação está online;
- conectar ao Bluehill;
- obter o estado atual do Bluehill;
- criar amostras;
- iniciar e parar testes;
- consultar resultados;
- guardar e fechar amostras;
- consultar medições;
- gerar logs de teste.

---

## 2. Tecnologias utilizadas

- C#
- .NET Framework 4.7.2
- ASP.NET Web API
- OWIN SelfHost
- Bluehill.API.dll
- Postman
- PHP/cURL para consumo externo

---

## 3. Arquitetura geral

Fluxo principal:

```text
Aplicação PHP / Postman / Frontend
        ↓
InstronBridgeSelfHost.exe
        ↓
Bluehill.API.dll
        ↓
Bluehill Universal
        ↓
Máquina Instron
```

O `.exe` cria um pequeno servidor HTTP local na porta `9000`.

Base URL padrão:

```http
http://localhost:9000
```

---

## 4. Estrutura principal do projeto

```text
InstronBridgeSelfHost
│
├── App_Start
│   └── WebApiConfig.cs
│
├── Callbacks
│   └── InstronCallback.cs
│
├── Controllers
│   └── InstronController.cs
│
├── InstronLogs
│   └── Logger.cs
│
├── Models
│   ├── CreateSampleRequest.cs
│   ├── SaveSampleRequest.cs
│   ├── PedidoBluehillDto.cs
│   └── InstronServiceState.cs
│
├── Services
│   └── InstronService.cs
│
├── Program.cs
├── Startup.cs
├── App.config
└── packages.config
```

---

## 5. Requisitos na máquina da Instron

A máquina onde o `.exe` será executado precisa ter:

```text
Bluehill Universal instalado
Bluehill.API.dll disponível
.NET Framework 4.7.2 ou superior
Permissão para executar o Bluehill
Permissão para criar ficheiros de log
```

Também é importante executar o `.exe` como administrador, principalmente durante os testes iniciais.

---

## 6. Compilação do projeto

No Visual Studio:

```text
Build → Configuration Manager
```

Selecionar:

```text
Release
x86
```

Depois executar:

```text
Build → Rebuild Solution
```

O projeto deve ser compilado em **x86**, porque a `Bluehill.API.dll` trabalha com arquitetura x86.

---

## 7. Publicação/execução na máquina da Instron

Após compilar, copiar todo o conteúdo da pasta:

```text
bin\x86\Release
```

Para a máquina da Instron, por exemplo:

```text
C:\InstronBridgeSelfHost
```

A estrutura final pode ficar assim:

```text
C:\InstronBridgeSelfHost
│
├── InstronBridgeSelfHost.exe
├── InstronBridgeSelfHost.exe.config
├── Bluehill.API.dll
├── Microsoft.Owin.dll
├── Newtonsoft.Json.dll
├── outras DLLs...
└── InstronLogs
```

Executar:

```text
InstronBridgeSelfHost.exe
```

De preferência:

```text
Botão direito → Executar como administrador
```

A consola deve mostrar algo parecido com:

```text
==========================================
 Instron Bridge API Self Host iniciado
==========================================

URL:
http://localhost:9000/
```

> Importante: a janela da consola precisa permanecer aberta. Ela é o servidor da API.

---

## 8. Logs

Os logs são guardados dentro da pasta onde o `.exe` está a ser executado.

Exemplo:

```text
C:\InstronBridgeSelfHost\InstronLogs\logs.txt
```

Durante o desenvolvimento, o caminho pode ser:

```text
bin\x86\Release\InstronLogs\logs.txt
```

Exemplo de log:

```text
2026-05-19 14:30:22 [INFO] SelfHost iniciado com sucesso.
2026-05-19 14:31:10 [INFO] Conectado ao Bluehill com sucesso.
```

---

# 9. Endpoints da API

Base URL:

```http
http://localhost:9000/api/instron
```

---

## 9.1 Health Check

Verifica se a API está online e mostra informações sobre a conexão.

### Request

```http
GET /health
```

### Exemplo

```http
GET http://localhost:9000/api/instron/health
```

### Response

```json
{
  "status": "online",
  "connected": true,
  "lastState": "ReadyToStartTest",
  "lastStatusCode": null,
  "lastStatusMessage": "Conectado ao Bluehill com sucesso."
}
```

### Para que serve

Este endpoint serve para verificar:

- se a API está ligada;
- se existe conexão com o Bluehill;
- qual foi o último estado conhecido;
- qual foi a última mensagem recebida.

---

## 9.2 Connect

Inicia o Bluehill, estabelece conexão com a API do Bluehill e mantém a referência em memória.

### Request

```http
POST /connect
```

### Exemplo

```http
POST http://localhost:9000/api/instron/connect
```

### Body

Não precisa de body.

### Response

```json
{
  "message": "Conectado ao Bluehill com sucesso."
}
```

### Observação

Este endpoint deve ser chamado antes dos endpoints que dependem do Bluehill.

---

## 9.3 State

Retorna o estado atual do Bluehill.

### Request

```http
GET /state
```

### Exemplo

```http
GET http://localhost:9000/api/instron/state
```

### Response

```json
{
  "state": "ReadyToStartTest"
}
```

### Para que serve

Permite saber em que estado o Bluehill se encontra no momento.

Exemplos de estados possíveis:

```text
BluehillStarting
BluehillHome
SampleOpened
ReadyToStartTest
Running
Calculating
```

---

## 9.4 Create Sample

Cria uma nova amostra no Bluehill a partir de um ficheiro de método.

### Request

```http
POST /create-sample
```

### Exemplo

```http
POST http://localhost:9000/api/instron/create-sample
```

### Body

```json
{
  "methodFilePath": "C:\\Users\\Public\\Documents\\Instron\\Bluehill Universal\\Templates\\metodo.im_tens"
}
```

### Response

```json
{
  "message": "Amostra criada com sucesso.",
  "result": "NoError"
}
```

### Para que serve

Este endpoint é usado quando a aplicação externa precisa criar uma nova amostra no Bluehill com base num método existente.

---

## 9.5 Start Test

Inicia o teste atual no Bluehill.

### Request

```http
POST /start-test
```

### Exemplo

```http
POST http://localhost:9000/api/instron/start-test
```

### Body

Não precisa de body.

### Response

```json
{
  "message": "Teste iniciado com sucesso.",
  "result": "NoError"
}
```

### Atenção

Este endpoint pode movimentar a máquina física. Usar apenas com operador presente e com o ensaio preparado.

---

## 9.6 Stop Test

Para o teste em execução.

### Request

```http
POST /stop-test
```

### Exemplo

```http
POST http://localhost:9000/api/instron/stop-test
```

### Body

Não precisa de body.

### Response

```json
{
  "message": "Teste parado com sucesso.",
  "result": "NoError"
}
```

### Para que serve

Permite interromper um teste em andamento através da API.

---

## 9.7 Results

Obtém os dados da tabela de resultados e estatísticas da amostra atualmente aberta no Bluehill.

### Request

```http
GET /results?tableNumber=1
```

### Exemplo

```http
GET http://localhost:9000/api/instron/results?tableNumber=1
```

### Response

```json
{
  "tableNumber": 1,
  "data": [
    ["Specimen", "Peak Load", "Extension"],
    [1, 520.4, 12.8]
  ]
}
```

### Observação importante

Este endpoint não devolve o histórico completo da máquina. Ele devolve os dados da tabela de resultados da amostra atualmente aberta no Bluehill.

Se retornar:

```json
{
  "tableNumber": 1,
  "data": null
}
```

pode significar que:

- não existe amostra aberta;
- a tabela 1 está vazia;
- o teste ainda não gerou resultados;
- os resultados ainda não foram calculados;
- a amostra aberta não possui resultados gravados.

---

## 9.8 Save Sample

Guarda a amostra atual.

### Request

```http
POST /save-sample
```

### Exemplo

```http
POST http://localhost:9000/api/instron/save-sample
```

### Body

```json
{
  "filePath": "C:\\Users\\Public\\Documents\\Instron\\Bluehill Universal\\Samples\\amostra1.is_tens"
}
```

### Response

```json
{
  "message": "Amostra guardada com sucesso.",
  "result": "NoError"
}
```

### Observação

Se o `filePath` for nulo, o Bluehill pode usar o caminho padrão.

---

## 9.9 Close Sample

Fecha a amostra atualmente aberta no Bluehill.

### Request

```http
POST /close-sample
```

### Exemplo

```http
POST http://localhost:9000/api/instron/close-sample
```

### Body

Não precisa de body.

### Response

```json
{
  "message": "Amostra fechada com sucesso.",
  "result": "NoError"
}
```

---

## 9.10 Measurement

Obtém uma medição específica do Bluehill.

### Request

```http
GET /measurement?measurementName=NOME&unit=UNIDADE
```

### Exemplo

```http
GET http://localhost:9000/api/instron/measurement?measurementName=Load&unit=Newtons
```

### Response

```json
{
  "measurement": "Load",
  "unit": "Newtons",
  "value": 120.45
}
```

### Observação

O nome da medição e a unidade precisam existir no Bluehill.

---

## 9.11 Disconnect

Fecha a conexão da API com o Bluehill.

### Request

```http
POST /disconnect
```

### Exemplo

```http
POST http://localhost:9000/api/instron/disconnect
```

### Body

Não precisa de body.

### Response

```json
{
  "message": "Conexão encerrada."
}
```

### Observação

Este endpoint limpa a conexão da API. Ele não deve ser tratado como botão obrigatório para fechar o Bluehill.

---

## 9.12 Teste

Endpoint simples para testar envio de JSON.

### Request

```http
POST /teste
```

### Exemplo

```http
POST http://localhost:9000/api/instron/teste
```

### Body

```json
{
  "nome": "Teste",
  "nif": "123456789",
  "email": "teste@email.com"
}
```

### Response

```json
{
  "mensagem": "Dados recebidos com sucesso",
  "dados": {
    "nome": "Teste",
    "nif": "123456789",
    "email": "teste@email.com"
  }
}
```

---

## 9.13 Teste Logs

Endpoint usado para testar a escrita de logs.

### Request

```http
POST /testeLogs
```

### Exemplo

```http
POST http://localhost:9000/api/instron/testeLogs
```

### Body

```json
{
  "nome": "Leonardo",
  "nif": "123456789",
  "email": "leonardo@email.com"
}
```

### Response

```json
{
  "sucesso": true,
  "mensagem": "Log criado com sucesso"
}
```

---

# 10. Fluxo recomendado de utilização

Fluxo básico:

```text
1. Abrir InstronBridgeSelfHost.exe
2. GET /health
3. POST /connect
4. GET /state
5. POST /create-sample
6. POST /start-test
7. GET /results
8. POST /save-sample
9. POST /close-sample
```

Para testes sem movimentar a máquina:

```text
1. Abrir InstronBridgeSelfHost.exe
2. POST /connect
3. Abrir manualmente no Bluehill uma amostra já existente com resultados
4. GET /results?tableNumber=1
```

---

# 11. Como consumir a API em PHP

A aplicação PHP pode consumir a API usando `cURL`.

Se o PHP estiver na mesma máquina da Instron:

```php
$baseUrl = "http://localhost:9000/api/instron";
```

Se o PHP estiver noutra máquina da mesma rede:

```php
$baseUrl = "http://IP-DA-MAQUINA-INSTRON:9000/api/instron";
```

Exemplo:

```php
$baseUrl = "http://172.21.0.194:9000/api/instron";
```

---

## 11.1 Health Check em PHP

```php
<?php

$url = "http://localhost:9000/api/instron/health";

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo "Erro cURL: " . curl_error($ch);
} else {
    $data = json_decode($response, true);

    echo "<pre>";
    print_r($data);
    echo "</pre>";
}

curl_close($ch);
```

---

## 11.2 Connect em PHP

```php
<?php

$url = "http://localhost:9000/api/instron/connect";

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_POST, true);

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo "Erro cURL: " . curl_error($ch);
} else {
    echo $response;
}

curl_close($ch);
```

---

## 11.3 Create Sample em PHP

```php
<?php

$url = "http://localhost:9000/api/instron/create-sample";

$body = [
    "methodFilePath" => "C:\\Users\\Public\\Documents\\Instron\\Bluehill Universal\\Templates\\metodo.im_tens"
];

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    "Content-Type: application/json"
]);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo "Erro cURL: " . curl_error($ch);
} else {
    $data = json_decode($response, true);

    echo "<pre>";
    print_r($data);
    echo "</pre>";
}

curl_close($ch);
```

---

## 11.4 Start Test em PHP

```php
<?php

$url = "http://localhost:9000/api/instron/start-test";

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_POST, true);

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo "Erro cURL: " . curl_error($ch);
} else {
    echo $response;
}

curl_close($ch);
```

---

## 11.5 Results em PHP

```php
<?php

$tableNumber = 1;

$url = "http://localhost:9000/api/instron/results?tableNumber=" . $tableNumber;

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo "Erro cURL: " . curl_error($ch);
} else {
    $data = json_decode($response, true);

    echo "<pre>";
    print_r($data);
    echo "</pre>";
}

curl_close($ch);
```

---

## 11.6 Measurement em PHP

```php
<?php

$measurementName = urlencode("Load");
$unit = urlencode("Newtons");

$url = "http://localhost:9000/api/instron/measurement?measurementName={$measurementName}&unit={$unit}";

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo "Erro cURL: " . curl_error($ch);
} else {
    $data = json_decode($response, true);

    echo "<pre>";
    print_r($data);
    echo "</pre>";
}

curl_close($ch);
```

---

# 12. Classe PHP reutilizável

```php
<?php

class InstronApiClient
{
    private string $baseUrl;

    public function __construct(string $baseUrl = "http://localhost:9000/api/instron")
    {
        $this->baseUrl = rtrim($baseUrl, "/");
    }

    private function request(string $method, string $endpoint, array $body = null)
    {
        $url = $this->baseUrl . $endpoint;

        $ch = curl_init($url);

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

        if ($method === "POST") {
            curl_setopt($ch, CURLOPT_POST, true);
        }

        if ($body !== null) {
            curl_setopt($ch, CURLOPT_HTTPHEADER, [
                "Content-Type: application/json"
            ]);

            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));
        }

        $response = curl_exec($ch);

        if (curl_errno($ch)) {
            throw new Exception(curl_error($ch));
        }

        curl_close($ch);

        return json_decode($response, true);
    }

    public function health()
    {
        return $this->request("GET", "/health");
    }

    public function connect()
    {
        return $this->request("POST", "/connect");
    }

    public function state()
    {
        return $this->request("GET", "/state");
    }

    public function createSample(string $methodFilePath)
    {
        return $this->request("POST", "/create-sample", [
            "methodFilePath" => $methodFilePath
        ]);
    }

    public function startTest()
    {
        return $this->request("POST", "/start-test");
    }

    public function stopTest()
    {
        return $this->request("POST", "/stop-test");
    }

    public function results(int $tableNumber = 1)
    {
        return $this->request("GET", "/results?tableNumber=" . $tableNumber);
    }

    public function saveSample(string $filePath)
    {
        return $this->request("POST", "/save-sample", [
            "filePath" => $filePath
        ]);
    }

    public function closeSample()
    {
        return $this->request("POST", "/close-sample");
    }

    public function measurement(string $measurementName, string $unit)
    {
        return $this->request(
            "GET",
            "/measurement?measurementName=" . urlencode($measurementName) . "&unit=" . urlencode($unit)
        );
    }

    public function disconnect()
    {
        return $this->request("POST", "/disconnect");
    }
}
```

---

## 12.1 Exemplo de uso da classe PHP

```php
<?php

require_once "InstronApiClient.php";

$instron = new InstronApiClient();

try {
    $health = $instron->health();
    print_r($health);

    $connect = $instron->connect();
    print_r($connect);

    $state = $instron->state();
    print_r($state);

} catch (Exception $e) {
    echo "Erro: " . $e->getMessage();
}
```

---

# 13. Cuidados de segurança

Esta API controla ou pode controlar uma máquina física. Portanto:

```text
Não expor esta API diretamente à internet
Executar apenas em rede local ou ambiente controlado
Restringir a porta 9000 na firewall
Permitir apenas máquinas autorizadas
Adicionar autenticação em versões futuras
Manter operador presente durante testes reais
Usar start-test apenas com segurança confirmada
```

---

# 14. Problemas conhecidos e soluções

## 14.1 `/results` retorna data null

Possíveis causas:

```text
Amostra não está aberta
Tabela de resultados vazia
Teste ainda não foi executado
Resultados ainda não foram calculados
Número da tabela incorreto
```

Solução:

```text
Abrir uma amostra com resultados ou executar um teste real antes de consultar /results.
```

---

## 14.2 Erro de arquitetura da Bluehill.API.dll

Erro típico:

```text
An attempt was made to load a program with an incorrect format.
```

Solução:

```text
Compilar o projeto em x86.
```

---

## 14.3 API não conecta ao Bluehill

Verificar:

```text
Bluehill Universal instalado
Bluehill.API.dll junto ao .exe
Caminho do Bluehill.exe correto
Executar como administrador
Fechar processos antigos do Bluehill
```

---

## 14.4 Porta 9000 não abre

Verificar:

```text
Se o .exe está aberto
Se a firewall permite a porta 9000
Se outro processo já está usando a porta
```

---

# 15. Observação final

Este projeto foi estruturado como SelfHost porque o IIS não manteve corretamente a conexão persistente com o Bluehill entre requests.

Com o SelfHost, a conexão permanece viva enquanto o `.exe` estiver aberto, tornando a solução mais adequada para integração com software industrial baseado em WCF/Named Pipes.

