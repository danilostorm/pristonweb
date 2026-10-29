# StormPT Launcher v0.3.1

A nova interface e aplicada **sobre o launcher Windows existente**. A rotina de manifestos assinados, verificacao SHA-256, instalacao de Game.exe, copia de seguranca e autoatualizacao de launcher.exe foi preservada.

## Visual

- Arte original aprovada pelo usuario, PNG RGB 1448x1086, preservada byte a byte (SHA-256 `f62b35428f71de5186170acfb84a8fd7762dc6071d8599a545f8113f3e3b93a9`).
- O launcher ajusta proporcionalmente a janela para caber na tela, com coordenadas de clique vinculadas ao mesmo sistema de referencia de 1448x1086.
- Botoes funcionais em `Iniciar Jogo`, `Verificar Arquivos`, configuracoes, minimizar, fechar e link de noticias.
- Noticias lidas de `launcher-news.txt`, atualizado pelo manifesto assinado.
- Status de conectividade consulta TCP `163.176.128.81:1997`. Quantidade de jogadores e dados nao verificaveis nao sao inventados.
- Barra de progresso acompanha o atualizador, ocultando o indicador ilustrativo originalmente desenhado na arte.
- Icone incorporado ao executavel a partir do emblema da arte.

## Atualizacao automatica

O arquivo de manifesto e assinado com RSA/SHA-256. O launcher antigo **v0.2.1** foi testado migrando automaticamente para **v0.3.0**, instalando `launcher-background.png` identico ao original, o executavel e as noticias, e reiniciando no novo launcher.

O fundo foi empacotado em ZIP sem perda e dividido em 46 partes por limite do fluxo de publicacao. O atualizador baixa as partes, reconstrui o ZIP, confere os dois SHA-256 e extrai a arte original sem modificar nenhum pixel.

O ajuste **v0.3.1** remove os ultimos fragmentos de texto ilustrativo que sobravam nas bordas das noticias e do indicador de progresso. A alteracao e apenas visual. Uma nova assinatura foi verificada localmente.

## Localizacao

- Compilado: `launcher/releases/launcher.exe`
- Fonte: `launcher/next/launcher-v0.3.1.cs`
- Feed: `launcher/feed/manifest.txt`, `launcher/feed/manifest.sig`
- Noticias: `launcher/feed/files/launcher-news.txt`
- Arte: `launcher/feed/files/bg.part00` ate `bg.part45` (extraida automaticamente para `launcher-background.png`)

**O problema de missao/funcao da Instrutora Amanda nao faz parte desta mudanca visual de launcher.** Nenhum NPC ou banco de dados foi alterado nesta versao.
