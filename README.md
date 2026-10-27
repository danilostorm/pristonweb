# StormPT — Priston Tale Web

Implantação do **Priston Tale no navegador** na VM **StormPT** (Windows 11, `192.168.30.238`), hospedada no AEROCOOL.

## Código original

O cliente, os dados e o servidor de jogo vêm de [sungam3d/priston-tale-web](https://github.com/sungam3d/priston-tale-web), mantidos no submódulo `upstream/`, fixado em um commit para instalações reproduzíveis.

O conteúdo do jogo pode ter direitos de terceiros e o projeto original não apresenta uma licença aberta explícita. Por isso, este repositório guarda a configuração de instalação e **referencia** os arquivos originais, em vez de republicar os binários e assets de terceiros.

Para baixar tudo (incluindo os arquivos do jogo):
```powershell
git clone --recurse-submodules https://github.com/danilostorm/pristonweb.git
```

## Instalação

No Windows 11, execute PowerShell **como administrador** a partir da pasta clonada:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-stormpt.ps1
```

Esse script habilita IIS / ASP.NET 4.8 / WebSockets, copia somente os arquivos web para `C:\inetpub\priston` e separa o executável e seus dados em `C:\PristonServer`. Configura o `Default Web Site` e mantém a porta TCP 10007 exclusivamente local.

Reinicie o Windows caso os componentes do IIS solicitem e execute o script novamente. Execute `C:\PristonServer\server.exe` e teste em `http://localhost/`.

**Importante:** um servidor acessível remotamente requer HTTPS com certificado válido, um nome DNS e configuração explícita de rede. Não exponha a porta TCP 10007; não publique arquivos da pasta do servidor. Antes de abrir ao público, avalie a segurança do código e do sistema de login. O primeiro login com ID inexistente cria uma conta.

## Operação

- Dados de jogadores: `C:\PristonServer\DataServer` — fazer backup regularmente, fora do repositório.
- Configurações: `C:\PristonServer\server.ini` (atualmente, 10 jogadores configurados no upstream).
- Logs e senhas nunca devem ser enviados ao GitHub.
- Após validar funcionamento, `C:\PristonServer\install_autostart.ps1` configura a inicialização automática como tarefa agendada.
- O site compartilha o mundo em `server.exe`; o arquivo `config.js` usa `serverUrl: 'ws.ashx'`.

## Diagnóstico

- `http://localhost/ws.ashx` deve responder com HTTP 400 "WebSocket connections only".
- `http://localhost/?debugnet` exibe o diagnóstico de conexão.
- Falha de login: verifique `server.exe`, TCP 10007 no host e WebSocket Protocol no IIS.
- Os navegadores exigem HTTPS para acesso remoto por causa de WebAssembly com threads/SharedArrayBuffer.

Para detalhes do projeto original, consulte [README do upstream](https://github.com/sungam3d/priston-tale-web/blob/main/README.md).

## Desempenho / carregamento no mapa

O servidor StormPT utiliza gzip no IIS e regras de cache e compressão no Cloudflare restritas a `stormpt.hoststorm.cloud`. O pacote inicial passou de cerca de 91 MB para 51 MB pela CDN e parte dos cenários/modelos reduz até 73% de transferência sem alterar os dados originais.

- [Relatório da otimização e resultados reais](docs/otimizacao-carregamento-2026-10-27.md)
- [Script de otimização do IIS](scripts/optimize-loading.ps1)
- [Service worker corrigido](overrides/sw.js)

O primeiro acesso ainda pode baixar recursos grandes específicos do mapa. Resultados de login/jogabilidade e medição ponta a ponta dependem de teste em navegador real.

## Interface fullscreen sem launcher

O endereço [stormpt.hoststorm.cloud](https://stormpt.hoststorm.cloud/) abre o cliente em modo janela completa, preservando a proporção original do jogo (4:3) e ocultando ferramentas de desenvolvedor. O carregamento inicial permanece visível em um indicador pequeno para não simular um jogo travado. Use `?debug=1` no URL para abrir logs e controles.

- [CSS responsivo](overrides/stormpt-ui.css)
- [Script de implantação idempotente](scripts/apply-fullscreen-ui.ps1)
- [Análise comparativa com OldPT e limites de desempenho](docs/analise-oldpt-e-interface-2026-10-27.md)

**Nota:** o projeto OldPT observado utiliza Three.js, WebGL e protocolo WebSocket próprio. A mudança de interface não transforma o cliente WebAssembly do StormPT em um novo engine WebGL nem elimina o download obrigatório dos recursos necessários ao mapa.
