<#
.SYNOPSIS
    Script de Deploy do Microsserviço de Autenticação (Infra + API)
.DESCRIPTION
    1. Build e Push da Imagem
    2. Cria o Namespace
    3. Sobe MongoDB
    4. Aplica ConfigMaps e Secrets
    5. Sobe a API e o Ingress
#>

$ErrorActionPreference = "Stop"
$DOCKER_USER = "fthalita91"
$IMAGE_NAME = "ms-auth-api"
$NAMESPACE = "auth"

function Write-Step {
    param($Message)
    Write-Host "`n🔹 $Message" -ForegroundColor Cyan
}

function Write-Success {
    param($Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

Write-Host "--- 0. Verificando Ingress Controller ---" -ForegroundColor Cyan
$ingress = kubectl get pods -n ingress-nginx --ignore-not-found
if (!$ingress) {
    Write-Host "Instalando Ingress Controller..." -ForegroundColor Yellow
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml
    Write-Host "Aguardando inicialização do Ingress (pode levar 30s)..."
    Start-Sleep -s 30
}

# --- 1. Build da Imagem ---
Write-Host "--- 1. Iniciando Build da Imagem ---" -ForegroundColor Cyan
docker build --pull=false -t "${DOCKER_USER}/${IMAGE_NAME}:latest" -f Dockerfile .

# --- 2. Push da Imagem ---
Write-Host "--- 2. Fazendo Push para o Docker Hub ---" -ForegroundColor Cyan
docker push "${DOCKER_USER}/${IMAGE_NAME}:latest"

# --- 3. Namespace ---
Write-Host "--- 3. Aplicando Namespace ---" -ForegroundColor Cyan
kubectl apply -f k8s/namespace.yaml

# --- 4. Infraestrutura (Banco) ---
Write-Step "4. Subindo Infraestrutura (Mongo)..."
Write-Host "   - Aplicando MongoDB..."
kubectl apply -f k8s/MongoDb/ -n $Namespace

Write-Host "⏳ Aguardando 10s para estabilização da infra..." -ForegroundColor Yellow
Start-Sleep -s 10

# --- 5. Configurações e Segredos ---
Write-Step "5. Aplicando Configurações e Segredos..."
kubectl apply -f k8s/secrets.yaml -n $Namespace
kubectl apply -f k8s/configmap.yaml -n $Namespace

# --- 6. API de Estoque ---
Write-Step "6. Realizando Deploy da API de Auth..."
kubectl apply -f k8s/deployment.yaml -n $Namespace
kubectl apply -f k8s/service.yaml -n $Namespace
kubectl apply -f k8s/hpa.yaml -n $Namespace

# Força o restart para garantir que peguem as configs novas (caso seja um update)
kubectl rollout restart deployment ms-auth-api -n $Namespace

# --- 7. Ingress ---
Write-Step "7. Configurando Rotas (Ingress)..."
kubectl apply -f k8s/ingress.yaml -n $Namespace

# --- 8. Validação Final ---
Write-Step "8. Status dos Pods:"
Start-Sleep -s 5
kubectl get pods -n $Namespace

Write-Host "`n🚀 DEPLOY FINALIZADO!" -ForegroundColor Green
Write-Host "Swagger: http://localhost/api/auth/" -ForegroundColor White
Write-Host "Health:  http://localhost/api/auth/health" -ForegroundColor White