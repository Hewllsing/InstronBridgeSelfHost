# Instron Bridge - Configuracao de Rede

A API agora inicia por padrao com o binding:

```text
http://+:9000/
```

Isso permite que a maquina Instron responda tanto localmente quanto pela rede.

## Antes de abrir o exe na maquina Instron

Abrir PowerShell como Administrador e executar uma vez:

```powershell
netsh http add urlacl url=http://+:9000/ user=Everyone
New-NetFirewallRule -DisplayName "Instron Bridge API 9000" -Direction Inbound -Protocol TCP -LocalPort 9000 -Action Allow
```

Se a regra `urlacl` ja existir, o primeiro comando pode devolver erro de registro duplicado. Nesse caso pode ignorar ou verificar com:

```powershell
netsh http show urlacl | findstr 9000
```

## Testes na maquina Instron

Com `InstronBridgeSelfHost.exe` aberto:

```powershell
Invoke-RestMethod http://localhost:9000/api/instron/health
Invoke-RestMethod http://localhost:9000/api/instron/state
Invoke-RestMethod "http://localhost:9000/api/instron/results?tableNumber=1"
```

## Testes a partir de outra maquina

Trocar `IP_DA_MAQUINA_INSTRON` pelo IP real:

```powershell
Invoke-RestMethod http://IP_DA_MAQUINA_INSTRON:9000/api/instron/health
Invoke-RestMethod http://IP_DA_MAQUINA_INSTRON:9000/api/instron/state
Invoke-RestMethod "http://IP_DA_MAQUINA_INSTRON:9000/api/instron/results?tableNumber=1"
```

No Postman, o teste tambem deve funcionar sem adicionar manualmente o header `Host`.

## Sobrescrever o binding

Se for preciso iniciar a API com outro endereco, ha duas opcoes.

Por argumento:

```powershell
.\InstronBridgeSelfHost.exe http://192.168.102.104:9000/
```

Por variavel de ambiente:

```powershell
$env:INSTRON_BRIDGE_URL = "http://192.168.102.104:9000/"
.\InstronBridgeSelfHost.exe
```

Para voltar ao comportamento antigo, usar `http://localhost:9000/`, mas isso volta a impedir chamadas vindas de outras maquinas.
