# StormPT - configuracao do nivel inicial

A fonte do servidor ja inicia com `int nStartUserLevel = 1;` em `SrcServer/src/Server/OnSever.cpp`, linha 2344, mas a rotina `StartUser_PresentItem_Init()` (linhas 2348-2365) sobrescreve esse padrao com a primeira coluna da tabela `StartUserDB.dbo.Level`.

Quando o arquivo de personagem ainda e provisoriamente vazio, a rotina de login em `OnSever.cpp` (linha ~27390) envia `nStartUserLevel` no campo `SParam` de `OPCODE_FAILRECORDDATA`.

Na VM Games, em 29 de outubro de 2026, `StartUserDB.dbo.Level.StartLevel` tinha valor 100 (apenas uma linha), causando personagens novos em level 100. O banco foi salvo com backup nativo e `RESTORE VERIFYONLY WITH CHECKSUM`. A atualizacao transacional de 100 para 1 teve uma linha afetada; o servidor foi reiniciado sem jogadores conectados e a porta interna 1997 voltou a escutar.

Backup do SQL da VM Games:
`C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\StormPT-StartUserDB-before-level1-20261029.bak`

Script idempotente para uso em nova instalacao/restauracao: `database/migrations/2026-10-29-new-character-level-1.sql`.

**Nao altere saves nem personagens ja criados.** Esta e uma configuracao do servidor, portanto os jogadores nao precisam baixar um `Game.exe` novo para essa mudanca.

## Atualizador

O launcher dos jogadores ja consulta e valida automaticamente o manifesto assinado ao iniciar e antes de jogar. O feed v0.2.1 esta em `launcher/feed/` neste ramo, com `launcher.exe` e o `Game.exe` correto na porta publica 1997. Mudancas somente no SQL nao requerem nova versao do cliente. Para uma futura atualizacao do cliente, os novos arquivos, hashes, versao e a assinatura RSA/SHA-256 precisam ser publicados no feed pela equipe. A chave privada fica no Ziel, nunca no cliente ou no GitHub.

Status de jogo: teste de um personagem *novo*, apos a alteracao do banco e o restart, ainda depende de login real; os testes de servico SQL/porta foram aprovados.
