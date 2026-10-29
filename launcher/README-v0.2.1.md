# StormPT launcher.exe v0.2.1

Launcher Windows para StormPT, compilado em Release x86 (.NET Framework 4.8).

## Instalacao inicial

1. Baixar **launcher.exe** em https://raw.githubusercontent.com/danilostorm/pristonweb/stormpt-launcher-v0.1.0/launcher/releases/launcher.exe
2. Colocar o arquivo na mesma pasta do Game.exe.
3. Executar launcher.exe. Ele verifica o manifesto automaticamente ao iniciar e novamente antes de jogar.

Depois da instalacao inicial, as novas versoes assinadas do launcher e dos arquivos do jogo sao aplicadas pelo proprio aplicativo. Nao ha necessidade de baixar patches .cmd.

## Atualizacoes automaticas

- Manifesto HTTPS: https://raw.githubusercontent.com/danilostorm/pristonweb/stormpt-launcher-v0.1.0/launcher/feed/manifest.txt
- Assinatura RSA/SHA-256 obrigatoria: manifest.sig, verificada contra chave publica incorporada no launcher.
- Arquivos conferidos por SHA-256 antes de serem instalados.
- Game.exe distribuido como ZIP compactado, em tres partes, com validacao do ZIP e do executavel extraido.
- Backup local do executavel substituido com extensao .stormpt.bak.
- Quando o launcher muda, um auxiliar aguarda o fechamento do processo anterior, substitui launcher.exe e reinicia.
- Settings.ini, game.ini e diretorios de saves nao sao substituidos pelo feed.
- Com falha de rede ou assinatura, mostra erro e nao inicia jogo automaticamente.
- Quando o jogo esta aberto, o launcher nao tenta substituir os executaveis.

## Testes (29/10/2026)

- Compilacao launcher.exe: zero erros, 24576 bytes.
- Teste na VM Games com pasta limpa: manifesto assinado validado e Game.exe instalado (SHA-256 F865F27126B0B6C56AE957832058F276D897D7739E915070EE6C59B84C47F69D).
- Teste da autoatualizacao: uma copia de teste do launcher com hash diferente substituiu a si mesma pela v0.2.1, com hash D5CFAA33E5DF2202311D881F7688A1D13A4E7C556069562BC66A69E275E06BE2, e reiniciou.
- Download publico de launcher.exe e do pacote foi testado via Oracle, com hash correspondente.
- Executavel instalado em C:\Elemental-Client\Cliente\launcher.exe na VM Games.

## Observacoes

Este feed esta em um **ramo de teste** do repositorio, e o Game.exe ainda nao foi validado em uma sessao completa entrando em Ricarten. O launcher atualiza os arquivos, mas nao corrige sozinho defeitos de login, NPCs ou persistencia do servidor.

Para publicar uma nova versao: atualizar os blobs em launcher/feed/files, editar o manifesto com nova versao/hash/tamanho e assiná-lo com a chave privada protegida, publicando manifesto e assinatura juntamente com os arquivos. A chave privada nunca deve ser enviada ao cliente nem ao GitHub.
