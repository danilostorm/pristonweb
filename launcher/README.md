# StormPT Launcher v0.1.0 (Windows)

Este ramo contem o instalador ZIP compilado na VM Games (Windows, .NET Framework 4.8), com `StormPTLauncher.exe`, `StormPTLauncher.ini` e instrucoes.

## Uso

Extraia o ZIP **na mesma pasta de Game.exe** e abra `StormPTLauncher.exe`. Os botoes Jogar StormPT, Verificar/Reparar e Abrir Pasta estao implementados.

## Atualizacoes

O launcher implementa manifesto HTTPS com assinatura RSA/SHA-256, verificacao SHA-256 por arquivo, backup antes de substituir arquivos e mecanismo de autoatualizacao do proprio launcher. **O feed de atualizacoes publico ainda nao foi publicado**, e `ManifestURL=` fica vazio por padrao. Nessa condicao, o launcher abre o jogo, mas nao aplica atualizacoes remotas.

## Falhas do jogo em investigacao

O launcher nao resolve o bug de personagem desaparecer nem a desconexao ao entrar em Ricarten. O servidor rejeita uma conexao secundaria sem associacao de conta; o save criado como provisoriamente vazio e removido na proxima selecao. O codigo do servidor e do cliente precisa de correcao e testes completos antes de distribuir um patch binario.

## Integridade

ZIP SHA256: `55d9844a792e415a1247b4ba32f218046d8c348e31d163882f360f49ea1d04bf`
EXE SHA256: `0ac195dab061f0ed1e61818c1da6026329f86f8fbffd9c124dc080e4d2b778a9`

A chave privada de assinatura nao acompanha o pacote.
