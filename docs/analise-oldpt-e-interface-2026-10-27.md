# OldPT vs StormPT — análise de cliente web e interface (27/10/2026)

## Comparação técnica verificável

Sites analisados:
- `https://oldpt.online/ptweb/`
- `https://stormpt.hoststorm.cloud/`

Inspeção limitada aos HTML, JavaScript, CSS e respostas HTTP **publicamente expostos** pelo servidor OldPT. Não reproduzimos seus binários, imagens, nem código proprietário.

| Característica | OldPT | StormPT atual |
|---|---|---|
| Interface inicial | `<canvas id="game">` ocupando a janela | Emscripten canvas 1024×768 com interface HTML de diagnóstico |
| Renderização | bundle JS incluindo Three.js r186 / WebGLRenderer | cliente adaptado para WebAssembly (`game.wasm` ~5 MB) |
| JS inicial | `/ptweb/assets/index-CaydiwnK.js` ~2.678.226 bytes | `game.js` ~284 KB, além do WebAssembly |
| CSS | `/ptweb/assets/index-CjJPKHNJ.css` ~158.407 bytes | CSS embutido e `stormpt-ui.css` |
| Downloads | cliente JS chama `fetch('/ptweb/pt/'+caminho)`, deduplica promessas em `Map` | manifesto com ~26 mil arquivos sob demanda mais pacote inicial `data_startup.pack` ~91 MB (antes de gzip) |
| Rede | classe JS `GameClient` usa `new WebSocket(this.url)` e envia hello com `protocolVersion:59` | servidor e ponte `ws.ashx` distintos, protocolo e cliente não intercambiáveis sem engenharia adicional |
| CSS de jogo | `#game{width:100vw;height:100vh}` | agora escala canvas 4:3 para caber na viewport preservando proporções |

O código original do OldPT não demonstrou `WebAssembly` / `.wasm` no bundle inspecionado. Logo, não é apenas trocar CSS ou HTML: são arquiteturas de cliente muito diferentes.

**Atenção:** a página inicial do OldPT ser pequena não comprova tempo exato até ficar jogável nem o volume baixado em todo o fluxo; parte dos dados ainda pode ser carregada conforme a necessidade.

## Interface fullscreen implementada

Arquivos neste repo:
- `overrides/stormpt-ui.css` — apresentação sem barra de launcher/log, tela cheia sem distorção e indicador compacto de download.
- `scripts/apply-fullscreen-ui.ps1` — backup, cópia de CSS e injeção idempotente do stylesheet e modo debug no HTML.
- `scripts/install-stormpt.ps1` — automaticamente reaplica o script de interface após a implantação do upstream, quando disponível.

Comportamento:
- `https://stormpt.hoststorm.cloud/` = interface sem controles técnicos visíveis.
- `https://stormpt.hoststorm.cloud/?debug=1` = reexibe barra e log para manutenção.
- O engine permanece em 1024×768, proporção 4:3. Telas largas terão bordas; celular em retrato terá espaços pretos acima e abaixo em vez de deformar o mundo.

### Validações no navegador

Browser: Chromium headless.

- Desktop 1440×900: canvas exibido em x=120, y=0, **1200×900**; #bar e #log CSS display none.
- Mobile 390×844: canvas exibido em x=0, y=275.75, **390×292.5**; #bar e #log CSS display none.
- O browser prosseguiu baixando dados; o comportamento real até login e mapa ainda requer teste humano.

## Para ficar tão rápido quanto o OldPT

O passo seguinte não é outra modificação superficial de CSS. Exige redução/reorganização do pacote inicial e, possivelmente, **um cliente novo WebGL/Three.js com carregamento progressivo de cenas** e adaptação legal/compatível do protocolo do nosso servidor. Isso implica desenvolvimento e testes separados (personagens, mapas, câmera, entradas, UI, áudio, combat e login). A parte visual e o cache HTTP já estão aplicados; a arquitetura do cliente ainda não foi reescrita.

**Não ocultar indiscriminadamente o indicador de download:** isso faz a tela parecer travada durante arquivos grandes; por isso foi minimizado mas mantido.
