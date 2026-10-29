# StormPT — correção de texto do mascote iniciante (29/10/2026)

**Release:** 0.3.2 (cliente, distribuído pelo manifesto RSA/SHA-256 do launcher).

O diálogo branco da captura é do pássaro de tutorial que acompanha personagens do level 1 ao 10, não da Instrutora Amanda.

**Causa encontrada:** `C:\Elemental-Source\Source\SrcGame\src\sinbaram\sinHelpPet.cpp`, `cHELPPET::DrawText()` configurava `SetFontTextColor32(RGB(255, 255, 255))` sobre uma janela branca criada por `g_Help.DrawBox(3,3,32,4)`.

**Mudança:** durante o texto do pássaro, usar `SetFontTextColor32(RGB(28, 36, 48))`; ao terminar `DrawText`, restaurar a cor padrão branca, preservando os demais controles. O conteúdo de `Image\SinImage\PetData\PetMessage.txt`, os arquivos de NPCs, os saves, o SQL e o servidor **não foram alterados**.

**Backup local dos arquivos antigos:**
`C:\Elemental-Setup\StormPT-PetTextFix-20261029\Game.exe.before` e
`C:\Elemental-Setup\StormPT-PetTextFix-20261029\sinHelpPet.cpp.before`.

**Build:** Game.exe Release x86, 0 erros de compilação; SHA-256 `CC542D7A2857A1ED8EA1C2EB356E3FE8361C19A60A8352FB13A22CC05BC56DE1`; tamanho 5.680.640 bytes.

**Pacote:** ZIP contém apenas `Game.exe`, 1.720.803 bytes; SHA-256 `68609E2AA455B487786B105C09E80110A2BCF3B36F258EB311CA7F23A957EF06`. Dividido em `petfix.part00-02`. O manifest e a assinatura são publicados juntos com os arquivos em `launcher/feed/`.

**Validação:** compilar, conferir o hash do ZIP, verificar assinatura RSA, baixar pelo HTTPS, extrair e comparar SHA-256; depois testar visualmente a leitura do diálogo do mascote num personagem até o level 10. A verificação visual no jogo pode exigir uma sessão real e não deve ser considerada confirmada apenas pelo build.

**Implantação:** os jogadores recebem no próximo início do `launcher.exe`; nenhuma substituição manual do `Game.exe` nem reinício do `Server33.exe` deve ser necessário.
