# StormPT 0.3.3 — progressão de personagem e Instrutora Amanda

**Data:** 29/10/2026

## Problema 1 — Personagem novo de nível 1 com aparência de terceira evolução e quests concluídas

**Causa confirmada:** em `Source/SrcGame/src/netplay.cpp`, função `CheckStartCharInfo(int lvl)`, chamada somente na inicialização de um personagem recém-criado (`OPCODE_FAILRECORDDATA`), a source colocava `lpCurPlayer->smCharInfo.ChangeJob = 3`, aplicava todos os bits de quests de nível e chamava `EndQuest_Code` para várias quests.

**Correção na VM Games:** dentro dessa função, usar `ChangeJob = 0`, `sinQuest_levelLog = 0` e não executar os laços que marcam todas as missões como concluídas. Preservar o resto da inicialização de nível, experiência, modelo, estatísticas e anticheat. **Não retroagir nem limpar quests de personagens existentes.** Personagens já salvos antes dessa correção não são modificados automaticamente.

Backup da source: `C:\Elemental-Setup\StormPT-Level1-Quest-Amanda-20261029\netplay.cpp.before`.

## Problema 2 — Amanda abrindo restauração de itens

**Causa confirmada:** a definição `C:\Servidor\GameServer\NPC\StormPT_InstrutoraAmanda.NPC` apontava para função 1024. A source `sinbaram/HaQuest.cpp`, em `HaCheckQuestNpc()`, confirma que 1024 chama `cRestaure.Open()`, enquanto 1014 chama `chaQuest.HaCheckQuestElementary(0)`.

**Correção aplicada:** alterar apenas a definição da Instrutora Amanda para **1014**, mantendo o mesmo arquivo usado por ambas as posições, em Ricarten e Pillai. Nenhum NPC existente ou posição de mapa foi removido ou alterado. O antigo NPC restaurador foi preservado com a função 1024.

A source do cliente já possui as sete quests originais `HAQUEST_CODE_ELEMENTARY_A` até `G`, diálogos em `Image/SinImage/Quest/haQuest*.sin`, lógica de recompensa em `Server/svrHaQuest.h` e chamadas a partir de `playmain.cpp`. Verificação de interação e das recompensas **dentro do jogo** ainda é necessária.

Backup do NPC: `C:\Elemental-Setup\StormPT-Level1-Quest-Amanda-20261029\Amanda.NPC.before`.

## Build e distribuição

- `Game.exe` Release x86: 5.680.640 bytes, SHA-256 `E5AA45E793E0C37876C18BEBADB605E3D95A85B038E8967AC0CF46E6DCA864B3`.
- O ZIP de 1.720.773 bytes tem SHA-256 `F2F9061B28255E9C3206DCFCA5A7AAB22224FD05335EDDE14DF2B3CB79E1C3FB`.
- Manifesto RSA-SHA256 versão **0.3.3**, com `launcher/feed/files/questfix.part00`, `questfix.part01`, `questfix.part02`.
- A instalação foi validada por `CheckAndUpdate()` do launcher e pelo launcher real na VM Games; o hash e o backup antigo foram verificados.
- O feed público é `https://raw.githubusercontent.com/danilostorm/pristonweb/stormpt-launcher-v0.1.0/launcher/feed/manifest.txt`.
- Relatório de QA e rollback na VM: `C:\Elemental-Setup\StormPT-Level1-Quest-Amanda-20261029\RELEASE-v033-report.txt`.

**Teste pendente:** criar um novo personagem level 1, verificar ausência do visual de classe evoluída e conversar com a Amanda em Ricarten/Pillai para começar a primeira quest. Não afirmar que a execução das sete missões foi testada apenas com inspeção de código e checagem de rede.
