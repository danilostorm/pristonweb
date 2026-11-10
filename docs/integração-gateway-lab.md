# Plano de ligação do StormPT Web ao Gateway e ao cliente Mobile

**Status (10/11/2026): documentação de integração, não implantação.**

A prévia Web do StormPT (Three.js/TypeScript/Vite) ainda precisa implementar autenticação real e sincronização com o mundo. O Gateway de laboratório foi desenvolvido em [danilostorm/pristonm](https://github.com/danilostorm/pristonm/tree/main/gateway) e inclui sessões, perfis, personagens e um adaptador inicial de `NFMsg.ReqRoleList` do Mobile.

## O que já está pronto fora do Web

- Módulo [StormPTGatewayClient.mjs](https://github.com/danilostorm/pristonm/blob/main/web-sdk/StormPTGatewayClient.mjs): login, identidade, listagem de personagens, logout; tokens apenas na memória do cliente, com testes automáticos aprovados.
- API .NET 8 em loopback: `/health`, `/v1/sessions`, `/v1/me`, `/v1/characters`, `/v1/mobile/role-list`, `/v1/logout`.
- Testes sintéticos do Gateway e do SDK Web no GitHub Actions aprovados.

## Integração prevista na prévia Web

1. **Não alterar a build publicada:** trabalhar com branch/cópia LAB isolada.
2. Após isolamento comprovado, hospedar a página Web via HTTPS e implementar proxy seguro **mesma origem** para `/v1/*`. O Gateway interno permanece em loopback, sem CORS público.
3. Reusar o módulo de integração para fazer login e exibir personagens da sessão, sem criar nova base de contas.
4. Implementar seleção do personagem e entrada no mapa mediante comando validado pelo servidor.
5. Implementar protocolo de sincronização do mundo (movimento, chat, combate, inventário), compatível com os dados autoritativos do StormPT Desktop.
6. Testar concorrência de sessões Desktop, Web e Mobile com contas descartáveis no LAB; não permitir gravar o mesmo personagem simultaneamente sem regras de arbitragem.

## Bloqueios críticos atuais

- Auditoria do LAB do AEROCOOL confirmou que o processo `Server33.exe` está conectado a bancos sem prefixo `MountLab_20261102_`. A origem do portal também referencia `AccountDB` e arquivos do DataServer padrão; a existência de bancos separados no SQL **não prova seu uso pelo servidor atual**.
- Disco C: do LAB com menos de 300 MB livres, com partição de recuperação após C:, tornando ampliação direta mais complexa.
- Gateway com autenticação real desativada por padrão e escutando somente em `127.0.0.1:18878`.
- Mobile possui APK legado assinado para testes, porém não executado com sucesso; faltam `.pak` e o script Android é bytecode compilado.

**Até que a auditoria de isolamento seja aprovada, não ligar a prévia Web a uma conta real nem modificar o servidor de produção.**