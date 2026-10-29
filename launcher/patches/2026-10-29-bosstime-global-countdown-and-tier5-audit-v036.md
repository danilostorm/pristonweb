# StormPT 0.3.6 — Boss Time, bosses de mapas e avaliação da Source Tier5

**Data:** 29/10/2026

## Entrega do Boss Time

- O servidor já possuía a agenda `[BossTime] TimeBoss=32` em `C:\Servidor\Configs\Sistemas.ini`; não foi necessário alterar o minuto.
- Antes, `Server/OnSever.cpp` tinha avisos de 5 e 1 minuto e aviso de nascimento **apenas para jogadores no mesmo mapa**. O aviso de nascimento saía antes de confirmar que `OpenMonster()` tinha sucesso.
- Alterações implantadas no `Server33.exe`:
  1. Avisos globais, aos personagens conectados e autenticados: "Nasce em 5 minutos", "Nasce em 1 minuto" e "Nasceu".
  2. Aviso de nascimento **apenas após retorno válido de `OpenMonster()`**, evitando falso anúncio quando o spawn falha ou sofre restrição (ex.: Babel).
  3. Registro de avisos no `RecordLog("BossTime", ...)` para auditoria.
  4. `StormPTNextScheduledBossSeconds()`: busca o horário mais próximo nos mapas carregados com master válido e informa segundos faltantes ao cliente no `OpCode::BOSS_TIME.LParam`; `WParam` preserva o minuto original. Os clientes anteriores ignoram `LParam`.
  5. Sincronização do contador na entrada do jogador e quando a hora/minuto do servidor muda.
  6. `fileread.cpp` agora reinicializa toda a estrutura `rsSTG_MONSTER_LIST` ao recarregar definições, evitando contagem antiga de bosses.

- O cliente transforma o campo estático `Boss Time: xx:32` em `Boss HH:MM:SS`, calculando os segundos decorridos pelo relógio monotônico local a partir da sincronização do servidor.
- A contagem exibe o **próximo horário agendado** dos mapas carregados. É uma janela planejada e não garantia de spawn: regras de respawn ou cooldown, por exemplo de Babel, ainda podem impedir uma aparição.
- Nenhum formato de save, banco SQL, mapa geométrico, item ou status de personagem foi modificado.

## Quatro definições antes comentadas que foram reativadas

| Arquivo | Boss | Cadastros e horários |
| --- | --- | --- |
| `dcave.ase.spm` | Guardião do Mal | Master Guardião do Mal + Guardião Maldito presentes; horas pares |
| `de-1.ase.spm` | Bauton | Master Bauton + Bargon presentes; horas pares |
| `de-1.ase.spm` | Fright Nemyun | Master Fright Nemyun + Cíclope presentes; horas ímpares |
| `dun-4.ase.spm` | Arqueiro Styx | Master Arqueiro Styx + Stygian presentes; horários originais preservados |

**Outras quatro permanecem comentadas, sem exclusão:**
- Dentuço Gigante — vinculado ao Coelhão de Páscoa, conteúdo sazonal.
- Espírito do Mal — definido para todas as 24 horas, podendo sobrecarregar o evento.
- Rashas — definido para todas as 24 horas, podendo desequilibrar os spawns.
- Lord — não foram encontradas definições correspondentes a `Lord` e `Aciente Golem` no catálogo dos monstros, risco de referência nula.

A Rosa Sangrenta já estava descomentada, mas sem horas, portanto não foi ativada nesta entrega.

## Build, backups, lançamento

- Backup de todos os arquivos originais, mapas e exes: `C:\Elemental-Setup\StormPT-BossTime-Tier5-Audit-20261029`.
- Servidor Release Win32 compilado com 0 erros. SHA-256: `33A2B5D41524A6E5171CC8897BE5993F2273FD443398BE64CB341F4AD43CA127`.
- Cliente Release Win32 compilado com 0 erros. SHA-256: `F85FA9B0290E78289DDC2E154BAC1EF6D0786A10EF42CE174A832B1EE01514AA`.
- ZIP cliente (apenas `Game.exe`): 1.722.356 bytes, SHA-256 `AA37B8FF24FB922882D7240C9D088350C5233081A79258AB3F8E5253352FAA05`.
- Manifesto 0.3.6 assinado via RSA-SHA256, com as partes `launcher/feed/files/boss.part00`, `boss.part01`, `boss.part02`. A chave privada fica no Ziel e **não** faz parte do repositório.
- O servidor novo teve startup lento: a primeira transação marcou timeout, mas o processo terminou de subir, permaneceu com PID 12688 e passou no TCP 1997 tanto na VM como pelo relay Oracle, com SQL Running. A auditoria no diretório de backup registra a reconciliação.
- A prova de spawn durante a janela do relógio `xx:32` e visualização do HUD pelos jogadores ainda precisa ser observada: compilação e conectividade **não** equivalem a teste completo de evento.

## Avaliação inicial da pasta Source Tier5

Pasta: `C:\Users\danil\Downloads\Source Tier5`

- Contém uma source cliente/servidor, um cliente de ~1,35 GB e pacote de servidor de ~1,05 GB, além de SQL de ~8,7 MB.
- `SrcGame/src/sinbaram/sinSkill.cpp`: `case 4` de `ChangeJob` habilita `ChangeJobSkillPlus = 21` (a source atual do StormPT limita ao `case 3` e 17).
- `SrcGame/src/sinbaram/sinQuest.cpp`: promoção para `ChangeJob=4` ocorre automaticamente a partir do level 80. Essa lógica **não** deve ser copiada sem adaptação, pois dispensaria etapas de quests no StormPT.
- `sinSkill.h` possui `CHANGE_JOB5` e skills do quinto tier (ex.: Soul Breaker, Talaria).
- O arquivo `Shared/smPacket.h` difere do atual. Necessária análise campo a campo antes de alterar pacotes e saves.
- Os pacotes trazem recursos gráficos de quinta evolução e ícones de Assassina e Xamã. A existência de recursos de classes não garante que todos os mecanismos de criação/autenticação e skill estejam plenamente compatíveis.
- Não se localizou sistema de montarias (`MountHandler`, `Riding`, `Horse`, `Montaria`) na source C++; arquivos `Mountain` são monstros, não montarias de jogadores.

**Próximas etapas para integração Tier 5 sem risco:**
1. Montar lab isolado com cópia da source Tier5, cliente e servidor, sem acesso de escrita aos saves em produção.
2. Fazer diff e matriz dos enums de job, slots de skills (17 → 21), pacotes, tabelas e habilidades.
3. Portar quarto estágio de `ChangeJob` e habilidades de Tier 5 entre cliente e servidor; preservar compatibilidade de saves.
4. Portar os modelos, animações, texturas, efeitos, ícones e quests necessários. Adicionar nova progressão com quests, **não promoção automática em level 80**.
5. Testar todas as classes e o balanceamento em QA, com criação de personagens, level-up, logout/login, quests e skills; somente então assinar update e implantar em produção.
6. Montarias, se desejadas, dependem de sistema novo ou source específica — não foram comprovadas neste pacote.

**Importante:** Tier 5 não foi implementada na produção nesta entrega; trata-se da auditoria de viabilidade. As mudanças em produção estão limitadas ao Boss Time e aos quatro spawns selecionados.
