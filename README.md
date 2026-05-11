# Sistema de Controle de Fluxo de Caixa - Merchant Processing

## Índice

1. [Descrição da Arquitetura](#1-descrição-da-arquitetura)
2. [Acesso ao Código, Preparação do Ambiente e Deploy](#2-acesso-ao-código-preparação-do-ambiente-e-deploy)

---

## 1. Descrição da Arquitetura

### Requisitos do exercício

1. **Funcional**: "1 (um) comerciante precisa controlar o seu fluxo de caixa diário com os lançamentos (débitos e créditos), também precisa de um relatório que disponibilize o saldo diário consolidado."

2. **Performance**: "O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair. Em dias de picos, o serviço de consolidado diário recebe 50 requisições por segundo, com no máximo 5% de perda de requisições."

### Arquitetura recomendada

![Arquitetura da Solução](https://github.com/tnnovak/exercicio-arq/blob/main/desenho-arq.jpg)

**Solução hospedada na AWS**

### Características da arquitetura

#### Escalabilidade
Microsserviços .NET 8 rodando em tasks no ECS. Todos os microsserviços podem rodar de 1 até 10 tasks com auto-scaling baseado em CPU / RequestCountPerTarget.

#### Balanceamento de carga
**Amazon API Gateway** como entry point único com autenticação JWT/Cognito, rate limiting (55 TPS), cache de respostas e WAF integrado. **Application Load Balancer** independente para cada serviço em VPC privada, conectados via VPC Link.

#### Estratégia de Cache
Uso do Redis (ElastiCache) para cache de saldo atual e consolidado diário (reduz carga do banco em até 10x). PostgreSQL RDS com read replica para separar leitura/escrita. Cache em três camadas: API Gateway (edge) → Redis (aplicação) → Database (replica).

#### Resiliência
ALB detecta instâncias não saudáveis e redireciona tráfego. Multi-instâncias de cada serviço (Multi-AZ no ECS). Retry policy com Polly. Idempotência garante retry seguro. Serviço de Lançamentos continua funcionando se Consolidado cair (independência via SQS).

#### Segurança
APIs protegidas via API Gateway com AWS IAM/Cognito, tokens JWT, HTTPS/TLS. Rate limit (proteção contra ataques de flood). WAF (Web Application Firewall). Criptografia em trânsito e em repouso. Logs no CloudWatch (auditoria).

#### Padrões arquiteturais
Serão 3 microsserviços: **API Lançamentos**, **API Consolidado** e **Worker Consolidador**, independentes e escaláveis.

- **CQRS**: Separação de comandos (write) e queries (read)
- **Event-Driven Architecture**: SQS para comunicação assíncrona
- **Publish-Subscribe**: Lançamentos publica eventos, Worker subscreve e atualiza totais
- **Materialized View**: ConsolidatedDaily pré-calculado para queries rápidas
- **Repository Pattern**: Domain models independentes do EF Core

#### Integração
Protocolos HTTPS/REST. Uso de JSON nas mensagens da fila SQS. Contratos de API com DTOs, versionamento (v1, v2) e Swagger/OpenAPI.

#### Confiabilidade
- **Idempotência**: Zero duplicação de transações
- **ACID**: Consistência de saldo garantida
- **Message durability**: SQS persiste mensagens (14 dias)
- **Retry policies**: Polly com exponential backoff
- **DLQ**: Mensagens problemáticas isoladas para análise

---

## 2. Acesso ao Código, Preparação do Ambiente e Deploy

### 2.1. Acesso ao repositório

**Repositório GitHub**: https://github.com/tnnovak/exercicio-arq

```bash
git clone https://github.com/tnnovak/exercicio-arq.git
cd exercicio-arq
```

**Estrutura atual do repositório**:
```
exercicio-arq/
├── README.md                                  # Documentação da arquitetura
├── IMPLEMENTATION.md                          # Detalhes de implementação
├── COMPLETE_SUMMARY.md                        # Sumário técnico completo
├── desenho-arq.jpg                           # Diagrama da arquitetura
├── desafio-arquiteto-software-out2024 1.pdf  # Especificação do desafio
├── MerchantProcessing.sln                    # Solution .NET 8
├── .gitignore                                # Exclusões do Git
├── docker-compose.yml                        # Ambiente local completo
├── src/
│   ├── MerchantProcessing.Domain/
│   │   ├── Entities/
│   │   │   ├── Account.cs
│   │   │   ├── Transaction.cs
│   │   │   ├── ConsolidatedDaily.cs
│   │   │   └── IdempotencyKey.cs
│   │   ├── Enums/
│   │   │   └── TransactionType.cs
│   │   ├── Events/
│   │   │   └── TransactionEvent.cs
│   │   └── DTOs/
│   │       └── ApiModels.cs
│   ├── MerchantProcessing.Infrastructure/
│   │   ├── Data/
│   │   │   └── MerchantDbContext.cs
│   │   └── Services/
│   │       ├── SqsService.cs
│   │       └── RedisCacheService.cs
│   ├── MerchantProcessing.Lancamentos.Api/
│   │   ├── Controllers/
│   │   │   └── LancamentosController.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   ├── MerchantProcessing.Consolidado.Api/
│   │   ├── Controllers/
│   │   │   └── ConsolidadoController.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   └── MerchantProcessing.Consolidator.Worker/
│       ├── Workers/
│       │   └── ConsolidatorWorker.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── Dockerfile
└── infrastructure/
    └── terraform/
        ├── main.tf
        ├── variables.tf
        └── outputs.tf
    └── infrastructure/                       # Terraform files
```

### 2.2. Como buildar e rodar localmente

**Pré-requisitos**: .NET 8 SDK, Docker Desktop

**Build da solução completa**:

```bash
# Restaurar dependências e buildar
cd /home/novak/TestOpah
dotnet restore
dotnet build

# Todos os projetos devem compilar com sucesso
```

**Executar com Docker Compose (RECOMENDADO)**:

```bash
# Subir todos os serviços (PostgreSQL, Redis, LocalStack, APIs e Worker)
docker-compose up -d

# Ver logs
docker-compose logs -f

# Ver logs de um serviço específico
docker-compose logs -f lancamentos-api
docker-compose logs -f consolidado-api
docker-compose logs -f consolidator-worker

# Parar todos os serviços
docker-compose down
```

**Rodar serviços individualmente (desenvolvimento)**:

```bash
# Rodar apenas as APIs (requer PostgreSQL e Redis rodando)
dotnet run --project src/MerchantProcessing.Lancamentos.Api     # Port 5001
dotnet run --project src/MerchantProcessing.Consolidado.Api     # Port 5002
dotnet run --project src/MerchantProcessing.Consolidator.Worker # Background
```

**Build de imagens Docker manualmente**:

```bash
docker build -f src/MerchantProcessing.Lancamentos.Api/Dockerfile -t lancamentos-api:latest .
docker build -f src/MerchantProcessing.Consolidado.Api/Dockerfile -t consolidado-api:latest .
docker build -f src/MerchantProcessing.Consolidator.Worker/Dockerfile -t consolidator-worker:latest .
```

**Pacotes NuGet instalados**:
- Microsoft.EntityFrameworkCore 8.0.7
- Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4
- AWSSDK.SQS 4.0.2.28
- StackExchange.Redis 2.12.14
- Microsoft.EntityFrameworkCore.Design 8.0.7

### 2.3. Testar o sistema localmente

#### 2.3.1. Preparar o ambiente

**Criar a conta de teste**:

```bash
# Criar dados de teste no banco
docker exec -i testopah-postgres-1 psql -U merchant -d merchantdb < tests/setup-test-data.sql
```

Isso cria uma conta com ID: `00000000-0000-0000-0000-000000000001`

#### 2.3.2. Testar via VS Code REST Client (RECOMENDADO)

**1. Instale a extensão REST Client no VS Code**

**2. Abra os arquivos de teste:**
- `src/MerchantProcessing.Lancamentos.Api/MerchantProcessing.Lancamentos.Api.http`
- `src/MerchantProcessing.Consolidado.Api/MerchantProcessing.Consolidado.Api.http`

**3. Clique em "Send Request" para executar:**

**Criar transação de crédito (R$ 100,00)**:
```http
POST http://localhost:5001/api/lancamentos
Content-Type: application/json
Idempotency-Key: credit-test-1234

{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "type": 0,
  "amount": 100.00,
  "idempotencyKey": "credit-test-12345"
}
```

**Criar transação de débito (R$ 50,00)**:
```http
POST http://localhost:5001/api/lancamentos
Content-Type: application/json
Idempotency-Key: debit-test-5678

{
  "accountId": "00000000-0000-0000-0000-000000000001",
  "type": 1,
  "amount": 50.00,
  "idempotencyKey": "debit-test-5678"
}
```

**Consultar saldo atual**:
```http
GET http://localhost:5002/api/consolidado/saldo/atual?accountId=00000000-0000-0000-0000-000000000001
```

#### 2.3.3. Testar via cURL (Terminal)

**Criar crédito de R$ 100**:

```bash
curl -X POST http://localhost:5001/api/lancamentos \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-$(date +%s)" \
  -d '{
    "accountId": "00000000-0000-0000-0000-000000000001",
    "type": 0,
    "amount": 100.00,
    "idempotencyKey": "credit-'$(date +%s)'"
  }'
```

**Criar débito de R$ 50**:

```bash
curl -X POST http://localhost:5001/api/lancamentos \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: debit-$(date +%s)" \
  -d '{
    "accountId": "00000000-0000-0000-0000-000000000001",
    "type": 1,
    "amount": 50.00,
    "idempotencyKey": "debit-'$(date +%s)'"
  }'
```

**Consultar saldo**:

```bash
curl "http://localhost:5002/api/consolidado/saldo/atual?accountId=00000000-0000-0000-0000-000000000001"
```

#### 2.3.4. Verificar resultados

**Ver transações no banco**:

```bash
docker exec -it testopah-postgres-1 psql -U merchant -d merchantdb -c \
  "SELECT * FROM \"Transactions\" ORDER BY \"Timestamp\" DESC LIMIT 5;"
```

**Ver saldo da conta**:

```bash
docker exec -it testopah-postgres-1 psql -U merchant -d merchantdb -c \
  "SELECT * FROM \"Accounts\" WHERE \"Id\" = '00000000-0000-0000-0000-000000000001';"
```

**Ver cache no Redis**:

```bash
docker exec -it testopah-redis-1 redis-cli KEYS "*"
```

#### 2.3.5. Documentação completa de testes

Para mais exemplos e troubleshooting, consulte: **[tests/TESTING_GUIDE.md](tests/TESTING_GUIDE.md)**

**Tipos de transação:**
- `type: 0` → Credit (Crédito - aumenta saldo)
- `type: 1` → Debit (Débito - diminui saldo)

---

### 2.4. Criar infraestrutura na AWS

**Usando Terraform**:

```bash
cd infrastructure/terraform
terraform init
terraform plan -out=tfplan
terraform apply tfplan
```

**Recursos criados**: VPC, RDS PostgreSQL + replica, ElastiCache Redis, ECS Cluster, ALBs, API Gateway, SQS, Cognito, CloudWatch, IAM roles e Security Groups.

### 2.5. Pipeline CI/CD e integração com AWS ECR

**GitHub Actions** (`.github/workflows/deploy.yml`):

```yaml
name: Deploy to AWS ECS
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v2
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: us-east-1
      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v1
      - name: Build and push
        run: |
          docker build -f src/MerchantProcessing.Lancamentos.Api/Dockerfile \
            -t ${{ steps.login-ecr.outputs.registry }}/lancamentos-api:${{ github.sha }} .
          docker push ${{ steps.login-ecr.outputs.registry }}/lancamentos-api:${{ github.sha }}
```

### 2.6. Criar e publicar imagens Docker no ECR

```bash
# Criar repositórios ECR
aws ecr create-repository --repository-name lancamentos-api
aws ecr create-repository --repository-name consolidado-api
aws ecr create-repository --repository-name consolidator-worker

# Autenticar no ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com

# Tag e push
docker tag lancamentos-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/lancamentos-api:v1.0.0
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/lancamentos-api:v1.0.0
```

### 2.7. Deploy ECR → ECS

**Deploy manual**:

```bash
# Atualizar task definition
aws ecs register-task-definition --cli-input-json file://task-definition.json

# Forçar novo deployment
aws ecs update-service --cluster merchant-cluster \
  --service lancamentos-api-service --force-new-deployment
```

**Deploy automático via pipeline**: Push na branch `main` → build e push para ECR → ECS service atualizado com rolling update (zero downtime).

**Verificação**:

```bash
aws ecs describe-services --cluster merchant-cluster --services lancamentos-api-service
aws logs tail /ecs/lancamentos-api --follow
curl https://api.merchant.com/health
```













