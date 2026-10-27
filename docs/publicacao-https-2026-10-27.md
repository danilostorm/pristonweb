# Publicação HTTPS — StormPT

Data: 27/10/2026.

Domínio público: https://stormpt.hoststorm.cloud/

- DNS Cloudflare (zona hoststorm.cloud): CNAME proxied para o túnel Cloudflare `Home Assistant AEROCOOL` (túnel existente, saudável).
- Ingress adicionada, preservando outras entradas: `stormpt.hoststorm.cloud -> http://192.168.30.238:80`.
- IIS Windows: `192.168.30.238:80`, diretório `C:\inetpub\priston`.
- Ponte WebSocket: `/ws.ashx?port=10007` encaminha ao processo Windows local.
- Porta 10007 permanece não acessível diretamente a partir do AEROCOOL.

## Testes remotos

| Endpoint | Resultado |
|---|---|
| `https://stormpt.hoststorm.cloud/` | 200 OK |
| `https://stormpt.hoststorm.cloud/game.wasm` | 200 OK, `application/wasm` |
| Upgrade WebSocket para `/ws.ashx?port=10007` | 101 Switching Protocols |
| Cabeçalhos COOP/COEP | Presentes |

**Pendente:** teste completo de cliente dentro do navegador, login, criação de personagem e movimentação no mundo. Testes de protocolo e HTTP não substituem testes de jogabilidade.

O túnel Cloudflare exclusivo `stormpt-priston-web-2026` foi criado, mas não é usado na rota ativa porque a provisionamento do conector demandaria transferência de credencial bloqueada pelo ambiente. Esse túnel pode ser excluído após auditoria, se não houver uso. A rota operacional usa o túnel existente `Home Assistant AEROCOOL`.
