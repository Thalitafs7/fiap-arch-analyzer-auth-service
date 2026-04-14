# Arch Analyzer - Auth Service

Serviço de autenticação e autorização para o projeto **Arch Analyzer**. Construído com **.NET 8** (C#) e **MongoDB**, projetado para rodar no **Amazon EKS** com deploy via **GitOps (ArgoCD)**.

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────────┐
│                          VPC (10.0.0.0/16)                          │
│                                                                      │
│                    ┌─────┐                                           │
│                    │ ALB │ ← HTTP :80                                │
│                    └──┬──┘                                           │
│                       │                                              │
│  ┌────────────────────▼───────────────────────────────────────────┐ │
│  │                            EKS Cluster                         │ │
│  │                                                                │ │
│  │  K8s Namespace: arch-analyzer-api                              │ │
│  │  ┌───────────────────────────┐                                 │ │
│  │  │ MS Auth API (Pod)         │ ← Autenticação / Autorização    │ │
│  │  │ - .NET 8                  │   (Geração de JWT, API Keys)    │ │
│  │  │ - Serilog (Logs)          │                                 │ │
│  │  └─────────┬─────────────────┘                                 │ │
│  │            │                                                   │ │
│  │            │ (Conexão via String de Conexão MongoDB)           │ │
│  │  ┌─────────▼─────────────────┐                                 │ │
│  │  │ MongoDB                   │                                 │ │
│  │  │ (StatefulSet ou Atlas)    │                                 │ │
│  │  └───────────────────────────┘                                 │ │
│  └────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

## Fluxo da Aplicação

1. **Geração de Tokens** → Cliente solicita autenticação → Auth API valida credenciais/chaves → Gera Token JWT.
2. **Validação de Acesso** → Outros microsserviços (ex: IA Service) validam tokens emitidos via Middleware de Autorização.
3. **Gestão de API Keys** → Coleção `apiKeys` no MongoDB armazena chaves de acesso.
4. **Logs** → Serilog estruturado em JSON captura eventos com CorrelationId para rastreabilidade.

## Estrutura do Projeto

```
ms-auth/
├── ms-auth.sln                      # Solução .NET
├── Dockerfile                       # Container build
├── docker-compose.yml               # Ambiente local com MongoDB
├── .gitignore
│
├── src/
│   ├── API/                         # Controllers, Program.cs, Middlewares (CorrelationId)
│   ├── Application/                 # Casos de Uso, Serviços de Aplicação
│   ├── Domain/                      # Entidades (ex: Token, ApiKey), Interfaces
│   └── Infrastructure/              # Repositórios MongoDB, Configurações
│
├── tests/
│   ├── UnitTests/                   # Testes unitários de domínio e aplicação
│   ├── IntegrationTests/            # Testes integrados com banco de dados em memória
│   └── BDD.Tests/                   # Testes BDD (Behavior-Driven Development)
│
└── k8s/
    ├── deployment.yaml              # Deploy do K8s
    ├── service.yaml                 # Serviço K8s
    ├── ingress.yaml                 # Regras de roteamento ALB/NGINX
    ├── configmap.yaml               # Variáveis de ambiente
    ├── aws-secret-template.yaml     # Secrets template
    ├── hpa.yaml                     # Horizontal Pod Autoscaler
    ├── namespace.yaml               # Namespace arch-analyzer-api
    └── MongoDb/                     # Manifests do MongoDB
```

## Modelo Multi-Repo

| Repositório | Conteúdo | Responsabilidade |
|---|---|---|
| `arch-analyzer-infra` | Terraform + GitOps config | Infraestrutura AWS |
| `arch-analyzer-api` (este) | Código + K8s manifests | API Autenticação |
| `arch-analyzer-ia` | Código + K8s manifests | Serviço de IA |

## Pré-requisitos

- .NET 8 SDK
- Docker & Docker Compose
- Kubectl (para deploy manual)
- Acesso ao MongoDB (local via Docker ou Cloud)

## Quick Start (Desenvolvimento Local)

```bash
# 1. Clone o repositório
git clone <repo-url>
cd ms-auth

# 2. Inicie o MongoDB e a API via Docker Compose
docker-compose up -d

# 3. Acesse o Swagger da API
# URL: http://localhost:5002/swagger/v1/swagger.json
# A API estará disponível em http://localhost:5002
```

> **Nota**: Você pode rodar a aplicação via IDE (Visual Studio / Rider) ou linha de comando (`dotnet run --project src/API/API.csproj`), garantindo que o MongoDB está rodando localmente (ex: `docker-compose up -d mongo`).

## Segurança

### Implementado
- **Controle de Acesso**: Validação de chave interna (`X-Internal-Key`) e geração de Tokens.
- **Rastreabilidade**: `CorrelationIdMiddleware` para correlacionar requisições através do cluster.
- **CORS**: Política permissiva em ambiente de desenvolvimento. Configuração refinada em produção.
- **Health Checks**: Endpoint `/health` implementado para o Kubernetes liveness/readiness probes.
- **Segredos K8s**: Configurações sensíveis (MongoDB Connection String, Chaves) isoladas via `aws-secret-template.yaml`.

## Decisões Técnicas

| Decisão | Justificativa |
|---|---|
| .NET 8 | Performance, suporte a longo prazo, ecossistema C# robusto |
| Arquitetura Limpa | Separação em camadas (API, Application, Domain, Infrastructure) para facilitar testes e manutenibilidade |
| MongoDB | Banco NoSQL flexível para armazenar chaves de acesso e metadados de tokens rapidamente |
| Serilog + CompactJson | Logs estruturados preparados para ingestão por ElasticSearch/CloudWatch |
| Testes Completos | Cobertura incluindo Unidade, Integração e BDD para garantir estabilidade do serviço crítico de autenticação |