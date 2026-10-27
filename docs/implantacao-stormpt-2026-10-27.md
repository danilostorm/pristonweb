# Estado da implantação StormPT — 27/10/2026

Máquina: **StormPT**, VM Windows 11 no AEROCOOL (Unraid). IP na LAN: `192.168.30.238`. Recursos configurados: 8 vCPU / 8 GiB RAM.

## Componentes e caminhos

- IIS 10 com ASP.NET 4.8 e WebSocket Protocol.
- Site IIS `Default Web Site`: `C:\inetpub\priston` (`DefaultAppPool`, CLR v4.0, Integrated).
- Servidor de mundo `C:\PristonServer\server.exe`, iniciado por tarefa agendada `PristonServer` como SYSTEM.
- Dados persistentes dos personagens: `C:\PristonServer\DataServer` (fazer backup fora desta VM).
- Área de preparação e fonte extraído: `C:\PristonDeploy\source\priston-tale-web-main`.
- ZIP temporário removido depois de instalado.
- Autostart da VM StormPT habilitado no Unraid.

## Verificações executadas

| Verificação | Resultado |
| --- | --- |
| `GET /` via rede AEROCOOL | HTTP 200 OK |
| `GET /game.wasm` | HTTP 200 OK / application/wasm |
| `GET /ws.ashx` sem Upgrade | HTTP 400 (comportamento esperado) |
| Handshake WebSocket `/ws.ashx?port=10007` | HTTP 101 Switching Protocols |
| Ponte WSS/TCP | Após HTTP 101, trafegou payload binário originado pelo servidor de jogo |
| `server.exe` | Processo ativo, TCP 10007 LISTEN no Windows |
| Acesso TCP 10007 diretamente do AEROCOOL | Bloqueado pelo firewall |
| Microsoft Defender (varredura personalizada de `server.exe`) | Sem detecções registradas |
| Tarefa agendada `PristonServer` | Running |

**Observação:** isto comprova entrega dos arquivos, protocolo WebSocket e conexão com o servidor. **Login e jogabilidade completos não foram validados em navegador.**

## Exposição pública pendente

O navegador exige **HTTPS com certificado válido** para executar o cliente WebAssembly com threads/SharedArrayBuffer fora do `localhost`. `http://192.168.30.238` pode servir a página mas não é origem segura para jogar num outro dispositivo.

Para disponibilizar na internet: definir domínio, HTTPS, liberar/encaminhar 443 ao IIS, verificar DNS/CGNAT e revisar segurança antes de abrir logins. Não abrir a porta TCP 10007 no roteador.

O script `scripts/install-stormpt.ps1` permite reinstalar / atualizar a aplicação sem excluir a pasta `DataServer`, mas recomenda-se backup prévio.

## Controle de origem

O conteúdo original não é republicado aqui: permanece referenciado no submódulo Git `upstream/`, fixado no commit `7017dda21aa1998f2b9b0cbf99954b57a8b90cb7`.
