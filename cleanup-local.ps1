$NAMESPACE = "auth"

Write-Host "--- INICIANDO LIMPEZA DO AMBIENTE: $NAMESPACE ---" -ForegroundColor Red

Write-Host "🗑️  Deletando Namespace '$NAMESPACE'..." -ForegroundColor Cyan
kubectl delete namespace $NAMESPACE

Write-Host "✅ Limpeza concluída!" -ForegroundColor Green