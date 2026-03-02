# Avaliação do estado atual e plano de evolução

Este documento resume o estado da implementação face aos requisitos do projeto de leitura de QR para faturas ATCUD e propõe um plano de evolução por milestones.

## 1) O que já está alinhado

### Base técnica
- **Frontend em Blazor WebAssembly .NET 8** já configurado no projeto cliente.
- **Backend em ASP.NET Core Web API .NET 8** já configurado.
- **EF Core com PostgreSQL** já presente no backend.
- **MinIO/S3** já integrado via `IStorageService`.

### Configuração por ambiente
- O frontend usa `AppConfig` ligado por `IOptions` e valida `ApiBaseUrl` no arranque.
- O backend usa `ApiConfig` com validações obrigatórias (storage, JWT, DB).
- Foi reforçada a abordagem sem hardcode no frontend com placeholders em `wwwroot/appsettings.json` e ficheiro específico `appsettings.Development.json`.

### Funcionalidade funcional já existente
- JWT + refresh token já existem com serviços/controladores de autenticação.
- Há estrutura de permissões/policies (`AdminOnly`, `OperadorOrAbove`, `RevisorOrAbove`).
- Já existe pipeline de QR no client com JS worker (`qrWorker.js`) e interop.
- Há componentes para upload, preview, validação e histórico.
- IndexedDB e serviços de sync também já existem como base.

## 2) Gaps face aos requisitos obrigatórios

### Gap A — UI stack
- O requisito define **MudBlazor**, mas o cliente ainda usa maioritariamente o template base (Bootstrap).
- Impacto: inconsistência com o standard definido e provável retrabalho de UI.

### Gap B — PWA/offline robusto
- A estrutura PWA existe (`manifest` + `service-worker`), mas falta confirmar estratégia **offline-first completa** (cache, fila outbox, conflitos e reintentos com backoff com observabilidade).
- Impacto: risco de comportamento inconsistente quando offline por longos períodos.

### Gap C — Performance e cancelamento
- O worker para decode existe, mas falta consolidar critérios de aceitação mensuráveis (<1.5s foto, <3s PDF típico) e botão de cancelamento sempre funcional.
- Impacto: pode haver regressões sem visibilidade objetiva.

### Gap D — Segurança e observabilidade enterprise
- Ainda não estão evidentes no código: **Serilog + sink**, política formal de mascaramento de dados sensíveis em log, e cifragem em repouso ponta-a-ponta para dados sensíveis.
- Impacto: não conformidade com requisitos de segurança/auditoria.

### Gap E — Módulos finais (histórico avançado/export/auditoria)
- Há base para histórico e export, mas os requisitos de filtros completos, auditoria funcional detalhada e formato final de integração do WS devem ser fechados.

## 3) Roadmap recomendado (próximas 4 iterações)

## Iteração 1 — Consolidar base e standards
1. Fechar “sem hardcode” com validação automatizada no CI (scan de URLs fora de ficheiros de ambiente permitidos).
2. Normalizar `AppConfig`/`ApiConfig` e documentar variáveis obrigatórias por ambiente.
3. Definir baseline de erros e logs sem dados sensíveis.

## Iteração 2 — Core de processamento (QR + PDF)
1. Fixar pipeline de decode (direto -> binarização -> alta resolução).
2. Implementar fallback de crop manual com UX objetiva.
3. Medir tempos por documento (render/decode/total).

## Iteração 3 — Offline-first completo
1. Fechar modelo IndexedDB (documento + ficheiro + outbox + tentativas).
2. Motor de sync com backoff exponencial + erros permanentes.
3. Ecrã de estado de sincronização por item.

## Iteração 4 — Segurança, auditoria e entrega
1. Serilog + sink (Seq/Elastic/DB) com correlação por documento.
2. Auditoria funcional completa por evento.
3. Export JSON final estável para integração WS.

## 4) Prioridades objetivas (ordem prática)
1. **Hardening de configuração e CI** (evita dívida técnica cedo).
2. **Validação do fluxo principal**: Upload/Câmara -> Decode -> Form -> Guardar rascunho.
3. **Offline + Sync** com testes de falha real.
4. **Logs/Auditoria/Segurança** antes de entrada em produção.

## 5) Critérios de “pronto para produção”
- Zero URL/endpoints hardcoded fora dos ficheiros de ambiente definidos.
- Fluxo offline funcional sem internet: capturar, ler, validar, guardar, e sincronizar depois.
- Métricas mínimas de desempenho cumpridas e monitorizadas.
- Auditoria consultável por documento, sem exposição de dados sensíveis em claro.
