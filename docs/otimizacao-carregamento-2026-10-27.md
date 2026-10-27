# Otimização de carregamento do StormPT — 27/10/2026

Ambiente: `https://stormpt.hoststorm.cloud/` → Cloudflare Tunnel → IIS em `192.168.30.238:80`.

## Problema medido

- O pacote inicial `data_startup.pack` tinha **90.956.979 bytes** transferidos sem HTTP compression.
- O manifesto `data_manifest.txt` lista aproximadamente **26.064 recursos** (3,94 GB somados), mas o navegador solicita os arquivos à medida que precisa deles; isto NÃO significa que o usuário baixa os 3,94 GB na entrada.
- Um cenário (`data/Field/AncientW/ancientW.smd`) tinha 11.152.206 bytes; antes os arquivos `application/octet-stream` não eram entregues pelo cache de borda (`cf-cache-status: DYNAMIC`).
- O service worker upstream `sw.js` conserva recursos por hash, mas seu caminho de streaming para `data_startup.pack` não removia cabeçalhos de codificação quando reconstruía a resposta.

## Mudanças aplicadas

1. **Cloudflare Cache Rules**, somente para `stormpt.hoststorm.cloud`:
   - `starts_with(http.request.uri.path, "/data/")`: cache de borda por 1800 segundos; respeitar TTL do navegador definido pela origem.
   - `http.request.uri.path in {"/data_startup.pack" "/data_manifest.txt" "/game.js" "/game.wasm"}`: cache por 600 segundos; respeitar TTL do navegador.
   - Recursos de login, página de conta e `ws.ashx` **não são cacheados por essas regras**.
2. **Cloudflare Compression Rule**, só para esse host e caminhos `/data/` e `/data_startup.pack`: utilizar gzip quando aceito pelo navegador.
3. **Windows IIS**: habilitada compressão estática de `application/octet-stream`, priorizada antes do padrão `*/* disabled` na coleção de MIME types. Configuração original foi copiada para `C:\PristonDeploy\applicationHost.before-gzip-20261027-005506.config`. O IIS reutiliza arquivos comprimidos depois da primeira geração.
4. **Service worker corrigido** em `overrides/sw.js`: para o fluxo do pacote inicial comprimido, remove `Content-Encoding` e `Content-Length` incompatíveis com o corpo que o navegador já descomprimiu; o instalador reaplica a substituição automaticamente se o checkout contiver `overrides/`.
5. **Cloudflare cache purge restrito ao hostname** `stormpt.hoststorm.cloud` para começar a utilizar as novas políticas sem afetar outros sites.

## Resultados observados

| Arquivo | Antes | Depois (Cloudflare gzip) | Redução aproximada |
|---|---:|---:|---:|
| `data_startup.pack` | 90,96 MB | 50,60 MB | 44% |
| `Field/AncientW/ancientW.smd` | 11,15 MB | 3,00 MB | 73% |
| `char/tmABCD/m1.smb` | 34,28 MB | 15,74 MB | 54% |
| `AssaLoadingFile.txt` | 12,3 KB | 1,8 KB | 85% |

A validação pública confirmou `cf-cache-status: HIT` na segunda requisição. SHA-256 do cenário recuperado pelo IIS e pelo Cloudflare comprimido foi idêntico, comprovando integridade do conteúdo. Os dados são exemplos reais, não uma medição de tempo total de entrada no mapa.

## Como reproduzir

Na VM Windows, com clone recursivo de `danilostorm/pristonweb`, execute como administrador:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\optimize-loading.ps1
```

As regras de Cloudflare não são aplicadas pelo script Windows; devem ser mantidas na zona `hoststorm.cloud`, escopadas ao hostname.

## Limites e próximas melhorias

O primeiro acesso ainda pode buscar muitos recursos diferentes (sobretudo mapas e personagens). O cache na CDN acelera arquivos já solicitados por outros jogadores; o service worker mantém os recursos que o usuário já baixou no próprio navegador. Para reduzir ainda mais o tempo de entrada inicial, será necessário analisar quais assets o jogo exige antes de renderizar o mapa e considerar pacotes prioritários / carregamento sob demanda, medindo desempenho em navegador real.

Nenhuma textura, modelo, mapa ou arquivo de save foi reduzido ou substituído no disco; a compressão é apenas de transporte HTTP. **Login, criação de personagem e tempo total até o mapa ainda precisam de medição em navegador real.**
