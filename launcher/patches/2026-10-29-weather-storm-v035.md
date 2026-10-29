# StormPT 0.3.5 — chuva, controles de clima e tempestades

Implementação no cliente Windows (29/10/2026), construída a partir da source e do sistema de atualização assinado existentes. A versão 0.3.4 foi preservada; o novo patch segue como 0.3.5.

## Funcionalidades

- Chuva ativada por padrão, migrando uma única vez os clientes antigos; a escolha posterior do jogador é preservada no `game.ini`.
- Nova aba **Clima** em Configurações, com dois checkboxes: **Chuva automática** e **Tempestades: raios e trovões**; clicar em **Salvar** persiste as escolhas.
- O sistema original de chuva continua com seus intervalos e restrições por mapa. Renderização utiliza as 19 texturas de `rain/` (a source procurava incorretamente em `game/sky/rain/`).
- Enquanto está chovendo em ambiente externo, uma tempestade ocasional apresenta relâmpago curto e de baixa opacidade, seguido por trovão com atraso. O som reutiliza `wav/thunder1.wav` e respeita a configuração de efeitos sonoros.
- Tempestades podem ser desligadas independentemente; desativar a chuva desativa também a tempestade. A configuração de qualidade baixa de textura pode suprimir a renderização de chuva para preservar FPS.

## Arquivos e persistência

Source Windows alterada:
`Settings/Settings.h`, `Settings/Settings.cpp`, `cSettingsWindow.h`, `cSettingsWindow.cpp`, `playsub.cpp`.

Assets novos distribuídos pelo feed:
`game/settings/climate.png` e `game/settings/climate_.png`.

Arquivo de configurações local ao usuário: `game.ini`, seção `[Game]`, campos `Weather`, `Storm`, `StormPTWeatherDefaultV034` e `StormPTStormDefaultV034`. O launcher **não sobrescreve game.ini**. A migração inicial é realizada pelo cliente e, após Salvar no menu, a escolha é preservada.

## Integridade / rollback

- Executável Game.exe Release Win32: 5.684.736 bytes; SHA-256 `B1F310486D472C4D2FFF906796903D298BDDE9EF16C99D4D1F7CC67E18478BE9`.
- Pacote ZIP Game.exe: 1.722.256 bytes; SHA-256 `C076CB43A2B536B28DB7C46C9D2D4EDB94E28CD6434C7CC08663E461172EE926`.
- Manifesto versão 0.3.5 assinado RSA/SHA-256. Arquivos sob `launcher/feed/`.
- Backup verificado antes das alterações:
`C:\Elemental-Setup\StormPT-Weather-Storm-v034-20261029`
- O Game.exe anterior permanece disponível para rollback, e nenhum banco SQL, personagem ou servidor foi modificado.

## Testes

Compilação Win32 concluída sem erros. SHA-256 do executável e pacote conferidos; assinatura RSA local verificada. A confirmação visual da chuva, dos relâmpagos e da interface ainda exige teste dentro do jogo, porque a compilação não garante a aparência final em todos os drivers gráficos.
