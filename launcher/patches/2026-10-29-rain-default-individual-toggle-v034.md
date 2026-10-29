# StormPT 0.3.4 — Chuva ligada por padrão, com opção individual

Data: 2026-10-29

## Funcionalidade
- A rotina original de chuva de `playmain.cpp` permanece intacta: chuva periódica, efeitos gráficos e sonoros, e restrições nos mapas (masmorras/gelo/SoD).
- Um novo checkbox **Chuva** fica na aba **Configurações → Vídeo/Gráficos**, no painel do próprio jogo.
- A opção carrega o valor local em `Game.ini` e é persistida ao clicar em **Salvar**; não depende de servidor nem de conta. Desligar chuva interrompe também o som do efeito.
- Para instalação antiga com `Weather=0` sem indicador de migração, a versão 0.3.4 grava uma única vez `[Game] Weather=1` e `StormPTWeatherDefaultV034=1`. Após essa migração, não substitui mais a escolha do usuário, inclusive quando for `Weather=0`.
- A atualização automática distribui **somente Game.exe e notícias**; ela não sobrescreve `Game.ini`, `Settings.ini`, contas ou saves. As preferências são mantidas nos próximos patches.

## Arquivos da source alterados na VM Games
- `C:\Elemental-Source\Source\SrcGame\src\Settings\Settings.cpp`: padrão ativo, migração única e persistência do marcador.
- `C:\Elemental-Source\Source\SrcGame\src\cSettingsWindow.h`: identificador `OBJID_Weather`.
- `C:\Elemental-Source\Source\SrcGame\src\cSettingsWindow.cpp`: checkbox de chuva, carregar, restaurar padrão e salvar.
- `C:\Elemental-Source\Source\SrcGame\src\playmain.cpp`: parar áudio de chuva quando desligada.

Backups SHA-256 conferidos antes das mudanças em:
`C:\Elemental-Setup\StormPT-Rain-Default-Toggle-v034-20261029\`

## Binários
- Compilação Release Win32: **zero erros**.
- `Game.exe` — 5.681.664 bytes; SHA-256 `789518C18364C3FBC96D2B1D415A26E43D4065AAD501D753CA7F399B440B430C`.
- ZIP de atualização — 1.721.149 bytes; SHA-256 `1B5E3E75B0C7F458BB4BF5FD4107292400AAD59CF085429C8ADF3D840C8415F7`.
- Partes do feed: `files/rain.part00`, `files/rain.part01`, `files/rain.part02`.
- Manifesto v0.3.4 assinado via RSA/SHA-256 pela chave protegida do Ziel; chave privada jamais incluída no repositório ou no launcher.

## Validação
Build e integridade do ZIP concluídos. Ainda é necessária uma confirmação visual em um cliente real para posição/legibilidade do checkbox e chuva no mapa; o efeito é periódico, não permanente. Ao testar alternância, abra Configurações, aba Vídeo, marque/desmarque **Chuva**, pressione **Salvar**, reinicie e confirme a persistência em `Game.ini`.
