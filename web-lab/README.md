# StormPT Web — protótipo de autenticação no LAB

**Status:** interface de teste do Gateway; **não é o jogo Web, não contém mapas 3D e não está publicada no domínio.**

Este protótipo foi criado em uma branch de laboratório para evitar alterar o cliente publicado. Em 10/11/2026 o repositório `pristonweb` na branch `main` continha apenas documentação e `.gitignore`, enquanto o caminho de fonte indicado no README (`C:\Elemental Priston Tale\web-client`) não existia na VM `StormPT-Mount-Lab-20261102` do AEROCOOL. Portanto esta interface é independente e não substitui um cliente de jogo existente.

## O que demonstra

- Página responsiva em PT-BR.
- Login por `POST /v1/sessions` do Gateway, sem armazenamento local de senhas ou tokens.
- Botões **Reservar** / **Liberar** na lista de personagens: criam uma reserva exclusiva de 90 segundos; uma segunda sessão recebe HTTP 409. Nenhuma reserva concede acesso ao mundo.
- Identidade via `/v1/me`, lista de personagens via `/v1/characters` e logout.
- O Gateway deve estar no mesmo domínio, encaminhado por HTTPS; o navegador não acessa SQL Server nem uma origem externa HTTP.
- O formulário é desabilitado se `/health` informar `realLoginConfigured=false` ou o serviço não estiver disponível.
- A listagem usa `textContent`, não insere nomes de personagens como HTML.
- Não cria contas, não entra no mapa, não implementa combate, inventário nem multiplayer. A reserva não sincroniza o cliente Desktop nem o servidor do jogo.

## Visualizar somente a interface local

Sem backend, a interface indica corretamente que o LAB está indisponível. Iniciar um servidor de arquivos estáticos de teste (somente localhost), a partir da raiz do repositório:

```bash
python -m http.server 8080 --bind 127.0.0.1 --directory web-lab
```

Abrir `http://127.0.0.1:8080` apenas para visualizar o layout. Essa prévia **não autentica**: `/health` não existe no servidor estático.

## Testes automáticos

```bash
node --check web-lab/app.mjs
node --test web-lab/tests/*.test.mjs
```

Os testes verificam a API cliente com dados fictícios, a renderização dos personagens, que a senha é limpa após tentativa de login, logout, texto seguro no DOM e o formato PT-BR. Nenhuma conta real é consultada. [Execuções GitHub Actions](https://github.com/danilostorm/pristonweb/actions).

## Bloqueios de integração real

1. O processo do servidor na VM LAB usa bancos `AccountDB` e outros nomes sem o prefixo `MountLab_20261102_`, e o Portal referencia o DataServer padrão. **Não foi comprovado o isolamento do estado persistente.**
2. A VM LAB está quase sem espaço em C:; não instalar componentes nela.
3. O Gateway .NET 8 atual escuta apenas em `127.0.0.1:18878` e o login real fica desativado por padrão. Não publicar a API sem TLS, isolamento, autorização segura e revisão do sistema de senhas.
4. A prévia Three.js/Vite anterior ainda precisa ser recuperada de sua fonte original e integrada a este fluxo antes de anunciar que o Web é jogável.
5. O APK Android ainda tem recursos e protocolo nativo pendentes.

**Nenhum deploy, banco de dados, produção ou DNS foi alterado.**