# Arch Analyzer - Auth Service

Serviço de autenticação e autorização para o projeto **Arch Analyzer**. Construído com **.NET 9** (C#) e **MongoDB**, projetado para rodar no **Amazon EKS** com deploy via **GitOps (ArgoCD)**.

## Resumo Arquitetural

- **Tipo**: Microserviço de autenticação, Clean Architecture + CQRS
- **Stack**: .NET 9, C#, MongoDB, MediatR, Serilog, FluentValidation
- **Infra**: Docker, K8s (EKS), HPA, NGINX Ingress, AWS Secrets Manager, Kustomize, GitHub Actions
- **Padrões**: Clean Architecture (4 camadas), CQRS, Mediator, Repository, Options Pattern
- **Auth**: API Key customizada (não JWT/OAuth). Chave interna protege geração de keys
- **DB**: MongoDB → coleção `apiKeys`
- **Sem filas/mensageria**. Única dependência externa = MongoDB

## Diagramas de Arquitetura

### 1. Arquitetura Interna (Camadas)

Direção de dependência entre camadas Clean Architecture.

```mermaid
graph TD
    subgraph API["API Layer"]
        AC["AuthController"]
        MW["CorrelationIdMiddleware"]
        EF["ExceptionFilter"]
        PR["Program.cs"]
    end

    subgraph Application["Application Layer"]
        GC["GenerateApiKeyCommand"]
        GH["GenerateApiKeyHandler"]
        VQ["ValidateApiKeyQuery"]
        VH["ValidateApiKeyHandler"]
        HB["HandlerBase"]
    end

    subgraph Domain["Domain Layer"]
        AKE["ApiKey Entity"]
        ENT["Entity Base"]
        IAKR["IApiKeyRepository"]
        DE["DomainException"]
    end

    subgraph Infrastructure["Infrastructure Layer"]
        AKR["ApiKeyRepository"]
        IKV["InternalKeyValidator"]
        LS["LogService"]
        CIS["CorrelationIdService"]
        CAKS["CurrentApiKeyService"]
        AKD["ApiKeyDocument"]
        MDB[("MongoDB")]
    end

    AC -->|MediatR| GH
    AC -->|MediatR| VH
    GH --> IAKR
    GH --> IKV
    VH --> IAKR
    AKR -.->|implements| IAKR
    AKR --> MDB
    GH --> HB
    VH --> HB
    HB --> LS
    AKE --> ENT
```

### 2. Fluxo de Geração de API Key

Fluxo completo do `POST /api/auth/apikey`.

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as CorrelationIdMiddleware
    participant AC as AuthController
    participant M as MediatR
    participant GH as GenerateApiKeyHandler
    participant IKV as InternalKeyValidator
    participant R as ApiKeyRepository
    participant DB as MongoDB

    C->>MW: POST /api/auth/apikey<br/>x-internal-key header
    MW->>MW: Extract/Generate CorrelationId
    MW->>AC: Forward request
    AC->>AC: Check header present
    alt Missing header
        AC-->>C: 401 Unauthorized
    end
    AC->>M: Send GenerateApiKeyCommand
    M->>GH: Handle
    GH->>IKV: IsValid(internalKey)
    alt Invalid key
        IKV-->>GH: false
        GH-->>C: 401 UnauthorizedException
    end
    IKV-->>GH: true
    GH->>GH: Generate 32-byte hex key
    GH->>R: CreateAsync(apiKey)
    R->>DB: InsertOne(ApiKeyDocument)
    DB-->>R: OK
    R-->>GH: ApiKey entity
    GH-->>AC: GenerateApiKeyResponse
    AC-->>C: 200 OK + API Key
```

### 3. Fluxo de Validação de API Key

Fluxo completo do `POST /api/auth/validate`.

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as CorrelationIdMiddleware
    participant AC as AuthController
    participant M as MediatR
    participant VH as ValidateApiKeyHandler
    participant R as ApiKeyRepository
    participant DB as MongoDB

    C->>MW: POST /api/auth/validate<br/>x-api-key header
    MW->>MW: Extract/Generate CorrelationId
    MW->>AC: Forward request
    AC->>AC: Check header present
    alt Missing header
        AC-->>C: 401 Unauthorized
    end
    AC->>M: Send ValidateApiKeyQuery
    M->>VH: Handle
    VH->>R: GetActiveByKeyAsync(apiKey)
    R->>DB: Find(key, not revoked)
    alt Key not found or revoked
        DB-->>R: null
        R-->>VH: null
        VH-->>AC: null
        AC-->>C: 401 Invalid API Key
    end
    DB-->>R: ApiKeyDocument
    R-->>VH: ApiKey entity
    VH-->>AC: ValidateApiKeyResponse
    AC-->>C: 200 Authorized
```

### 4. Topologia de Infraestrutura

Arquitetura K8s + AWS para deploy.

```mermaid
graph TD
    subgraph AWS["AWS Cloud"]
        subgraph EKS["EKS Cluster"]
            subgraph NS["Namespace: auth"]
                ING["NGINX Ingress<br/>/api/auth"]
                SVC["ClusterIP Service<br/>:5002"]
                DEP["Deployment<br/>ms-auth-api<br/>2-5 replicas"]
                HPA["HPA<br/>70% CPU target"]
                CM["ConfigMap<br/>ms-auth-config"]
                SEC["Secret<br/>ms-auth-secret"]
                INIT["Init Container<br/>secrets-sync"]
            end
            subgraph MONGO_NS["Namespace: auth - MongoDB"]
                MONGODEP["MongoDB 6.0<br/>Deployment"]
                MONGOSVC["MongoDB Service<br/>:27017"]
                PVC["PVC 1Gi"]
            end
        end
        SM["AWS Secrets Manager<br/>arch-analyzer/auth/mongo"]
        ECR["ECR<br/>arch-analyzer-auth"]
    end

    subgraph CI["GitHub Actions"]
        BUILD["Build + Push Docker"]
    end

    ING --> SVC
    SVC --> DEP
    HPA -.->|scales| DEP
    DEP --> CM
    DEP --> SEC
    INIT -->|fetch| SM
    DEP --> MONGOSVC
    MONGOSVC --> MONGODEP
    MONGODEP --> PVC
    BUILD -->|push image| ECR
    ECR -.->|pull| DEP
```

### 5. Máquina de Estados - Exception Handling

Mapeamento do ExceptionFilter.

```mermaid
stateDiagram-v2
    [*] --> ExceptionThrown
    ExceptionThrown --> ValidationException: FluentValidation
    ExceptionThrown --> UnauthorizedException: Auth failure
    ExceptionThrown --> DomainException: Business rule
    ExceptionThrown --> UnhandledException: Unknown

    ValidationException --> HTTP400: field errors
    UnauthorizedException --> HTTP401: message
    DomainException --> HTTP400: domain message
    UnhandledException --> HTTP500: internal error

    HTTP400 --> [*]
    HTTP401 --> [*]
    HTTP500 --> [*]
```

### 6. Cross-Cutting Concerns

Propagação de logging + correlation.

```mermaid
graph LR
    subgraph Request["Incoming Request"]
        H["X-Correlation-ID header"]
    end

    subgraph Middleware["CorrelationIdMiddleware"]
        EX["Extract or Generate ID"]
    end

    subgraph Services["Scoped Services"]
        CIS["CorrelationIdService"]
        CAKS["CurrentApiKeyService"]
    end

    subgraph Logging["LogService"]
        LE["LogEntryDto"]
    end

    subgraph Output["Serilog Output"]
        JSON["CompactJSON to stdout"]
    end

    H --> EX
    EX --> CIS
    CIS --> LE
    CAKS --> LE
    LE -->|"class, method, correlationId,<br/>traceId, spanId, apiKey"| JSON
```

---

## Arquitetura (Visão Simplificada)

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
│  │  K8s Namespace: auth                                           │ │
│  │  ┌───────────────────────────┐                                 │ │
│  │  │ MS Auth API (Pod)         │ ← Autenticação / Autorização    │ │
│  │  │ - .NET 9                  │   (Geração de API Keys)         │ │
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

1. **Geração de API Keys** → Cliente envia `x-internal-key` → Auth API valida → Gera key hex 32 bytes → Persiste MongoDB.
2. **Validação de Acesso** → Outros microsserviços enviam `x-api-key` → Auth API verifica se key existe e não está revogada.
3. **Gestão de API Keys** → Coleção `apiKeys` no MongoDB armazena chaves de acesso.
4. **Logs** → Serilog estruturado em JSON captura eventos com CorrelationId para rastreabilidade.

## Estrutura do Projeto

```
ms-auth/
├── ms-auth.sln                      # Solução .NET
├── Dockerfile                       # Container build (multi-stage)
├── docker-compose.yml               # Ambiente local com MongoDB
├── .gitignore
│
├── src/
│   ├── API/                         # Controllers, Program.cs, Middlewares (CorrelationId)
│   ├── Application/                 # Commands, Queries, Handlers (CQRS via MediatR)
│   ├── Domain/                      # Entidades (ApiKey), Interfaces, Exceptions
│   └── Infrastructure/              # Repositórios MongoDB, Logging, Options
│
├── tests/
│   ├── UnitTests/                   # Testes unitários de domínio e aplicação
│   ├── IntegrationTests/            # Testes integrados com banco de dados em memória
│   └── BDD.Tests/                   # Testes BDD (Behavior-Driven Development)
│
└── k8s/
    ├── deployment.yaml              # Deploy do K8s (2 replicas, secrets-sync init)
    ├── service.yaml                 # ClusterIP :5002
    ├── ingress.yaml                 # NGINX Ingress /api/auth
    ├── configmap.yaml               # Variáveis de ambiente
    ├── aws-secret-template.yaml     # Secrets template (AWS Secrets Manager)
    ├── hpa.yaml                     # HPA 2-5 replicas, 70% CPU
    ├── namespace.yaml               # Namespace auth
    ├── kustomization.yaml           # Kustomize com image override
    └── MongoDb/                     # Manifests do MongoDB (deployment, service, pvc)
```

## Modelo Multi-Repo

| Repositório | Conteúdo | Responsabilidade |
|---|---|---|
| `arch-analyzer-infra` | Terraform + GitOps config | Infraestrutura AWS |
| `arch-analyzer-auth` (este) | Código + K8s manifests | API Autenticação |
| `arch-analyzer-ia` | Código + K8s manifests | Serviço de IA |

## Pré-requisitos

- .NET 9 SDK
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

## Segurança

### Implementado
- **Controle de Acesso**: Validação de chave interna (`x-internal-key`) para geração e `x-api-key` para validação.
- **Rastreabilidade**: `CorrelationIdMiddleware` para correlacionar requisições através do cluster.
- **CORS**: Política permissiva em ambiente de desenvolvimento. Configuração refinada em produção.
- **Health Checks**: Endpoint `/health` implementado para o Kubernetes liveness/readiness probes.
- **Segredos K8s**: Configurações sensíveis (MongoDB Connection String, Chaves) isoladas via AWS Secrets Manager + init container.

## Decisões Técnicas

| Decisão | Justificativa |
|---|---|
| .NET 9 | Performance, suporte a longo prazo, ecossistema C# robusto |
| Clean Architecture + CQRS | Separação em camadas + segregação comando/query via MediatR |
| MongoDB | Banco NoSQL flexível para armazenar chaves de acesso rapidamente |
| Serilog + CompactJson | Logs estruturados preparados para ingestão por ElasticSearch/CloudWatch |
| HPA (2-5 pods) | Auto-scaling baseado em CPU para lidar com picos de validação |
| AWS Secrets Manager | Rotação segura de secrets sem rebuild de imagem |
