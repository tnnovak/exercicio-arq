# Guia de Testes - MerchantProcessing

Este guia mostra como testar o sistema localmente.

## 1. Preparar o Ambiente

### 1.1. Subir os serviços com Docker Compose

```bash
cd /home/novak/TestOpah
docker-compose up -d
```

Isso vai subir:
- PostgreSQL (porta 5432)
- Redis (porta 6379)
- LocalStack/SQS (porta 4566)
- Lancamentos API (porta 5001)
- Consolidado API (porta 5002)
- Consolidator Worker (background)

### 1.2. Criar o schema do banco de dados

```bash
# Via EF Core Migrations (recomendado)
dotnet ef migrations add InitialCreate --project src/MerchantProcessing.Infrastructure --startup-project src/MerchantProcessing.Lancamentos.Api
dotnet ef database update --project src/MerchantProcessing.Infrastructure --startup-project src/MerchantProcessing.Lancamentos.Api
```

**OU** executar manualmente se não houver migrations:

```bash
# Conectar ao PostgreSQL
docker exec -it testopah-postgres-1 psql -U merchant -d merchantdb

# Executar script de criação de schema manualmente se necessário
```

### 1.3. Criar dados de teste

```bash
# Executar script SQL de setup
docker exec -i testopah-postgres-1 psql -U merchant -d merchantdb < tests/setup-test-data.sql
```

Isso cria uma conta de teste com ID: `00000000-0000-0000-0000-000000000001`

---

## 2. Executar Testes via VS Code REST Client

### 2.1. Instalar extensão

Instale a extensão **REST Client** no VS Code.

### 2.2. Testar API de Lançamentos

Abra o arquivo: `src/MerchantProcessing.Lancamentos.Api/MerchantProcessing.Lancamentos.Api.http`

Clique em **"Send Request"** sobre cada requisição:

1. **Health Check** - Verifica se a API está rodando
2. **Create Credit** - Adiciona R$ 100,00 de crédito
3. **Create Debit** - Remove R$ 50,00 de débito
4. **Test Idempotency** - Testa se a mesma requisição retorna o mesmo resultado

### 2.3. Testar API de Consolidado

Abra o arquivo: `src/MerchantProcessing.Consolidado.Api/MerchantProcessing.Consolidado.Api.http`

1. **Get Current Balance** - Consulta saldo atual (cache Redis)
2. **Get Daily Consolidated** - Consulta relatório diário

---

## 3. Executar Testes via cURL

### 3.1. Criar transação de crédito

```bash
curl -X POST http://localhost:5001/api/lancamentos \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-$(date +%s)" \
  -d '{
    "accountId": "00000000-0000-0000-0000-000000000001",
    "type": 1,
    "amount": 100.00,
    "idempotencyKey": "credit-test-'$(date +%s)'"
  }'
```

### 3.2. Criar transação de débito

```bash
curl -X POST http://localhost:5001/api/lancamentos \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: debit-$(date +%s)" \
  -d '{
    "accountId": "00000000-0000-0000-0000-000000000001",
    "type": 2,
    "amount": 50.00,
    "idempotencyKey": "debit-test-'$(date +%s)'"
  }'
```

### 3.3. Consultar saldo atual

```bash
curl -X GET "http://localhost:5002/api/consolidado/saldo/atual?accountId=00000000-0000-0000-0000-000000000001" \
  -H "Accept: application/json"
```

### 3.4. Consultar consolidado diário

```bash
curl -X GET "http://localhost:5002/api/consolidado/diario/2026-05-11?accountId=00000000-0000-0000-0000-000000000001" \
  -H "Accept: application/json"
```

---

## 4. Verificar Logs

### 4.1. Logs da API de Lançamentos

```bash
docker-compose logs -f lancamentos-api
```

### 4.2. Logs do Worker (Hangfire)

```bash
docker-compose logs -f consolidator-worker
```

### 4.3. Logs do SQS (LocalStack)

```bash
docker-compose logs -f localstack
```

---

## 5. Verificar Dados no Banco

```bash
# Conectar ao PostgreSQL
docker exec -it testopah-postgres-1 psql -U merchant -d merchantdb

# Consultar conta
SELECT * FROM "Accounts" WHERE "Id" = '00000000-0000-0000-0000-000000000001';

# Consultar transações
SELECT * FROM "Transactions" WHERE "AccountId" = '00000000-0000-0000-0000-000000000001' ORDER BY "Timestamp" DESC;

# Consultar consolidado diário
SELECT * FROM "ConsolidatedDaily" WHERE "AccountId" = '00000000-0000-0000-0000-000000000001';

# Sair do PostgreSQL
\q
```

---

## 6. Verificar Cache Redis

```bash
# Conectar ao Redis
docker exec -it testopah-redis-1 redis-cli

# Ver todas as chaves
KEYS *

# Ver saldo em cache
GET account:00000000-0000-0000-0000-000000000001:balance

# Sair do Redis
exit
```

---

## 7. Verificar Fila SQS (LocalStack)

```bash
# Listar mensagens na fila
aws --endpoint-url=http://localhost:4566 sqs receive-message \
  --queue-url http://localhost:4566/000000000000/transaction-events \
  --max-number-of-messages 10
```

---

## 8. Testar Hangfire Job (Consolidação Diária)

O job roda automaticamente às 23:59 UTC, mas você pode testá-lo manualmente:

### 8.1. Acessar Hangfire Dashboard (se habilitado)

```
http://localhost:5001/hangfire
```

### 8.2. Executar job manualmente via código

Adicione endpoint temporário em `Program.cs`:

```csharp
app.MapPost("/test/consolidate", async (DailyConsolidationJob job) =>
{
    await job.ExecuteAsync();
    return Results.Ok("Job executed");
});
```

---

## 9. Limpar Ambiente

```bash
# Parar todos os containers
docker-compose down

# Parar e remover volumes (limpa dados do banco)
docker-compose down -v
```

---

## 10. Tipos de Transação

- `type: 1` → **Credit** (Crédito - aumenta saldo)
- `type: 2` → **Debit** (Débito - diminui saldo)

---

## 11. Troubleshooting

### APIs não respondem
```bash
docker-compose ps
docker-compose logs lancamentos-api
docker-compose logs consolidado-api
```

### Banco de dados não conecta
```bash
docker-compose logs postgres
# Verificar se porta 5432 está disponível
```

### Redis não conecta
```bash
docker-compose logs redis
# Verificar se porta 6379 está disponível
```

### Worker não processa mensagens
```bash
docker-compose logs consolidator-worker
# Verificar conexão com SQS/LocalStack
```
