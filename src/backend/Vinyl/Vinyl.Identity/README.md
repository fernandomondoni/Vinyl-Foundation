# Vinyl.Identity

Serviço responsável pelo domínio de identidade e autorização do Vinyl.

## Responsabilidades

- Amazon Cognito User Pool para autenticação e emissão de tokens;
- Usuário, perfil, workspace, membership, role e permission no domínio do Vinyl;
- Validação de access tokens OIDC/JWT nas APIs;
- Provisionamento do usuário local no primeiro acesso autenticado;
- Persistência em PostgreSQL com Marten quando configurada;
- Persistência em memória somente para desenvolvimento local.

## Executar localmente

O ambiente `Development` usa persistência em memória e mantém a autenticação protegida. Sem um Cognito configurado, endpoints protegidos respondem `401`.

```bash
dotnet run --project src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj
```

### PostgreSQL local com Docker

Para testar a persistência real do Identity localmente, suba o PostgreSQL a partir da raiz do repositório:

```bash
docker compose up -d vinyl-identity-postgres
docker compose ps
```

O Compose cria o banco `vinyl_identity`, com usuário `vinyl`, na porta `5432`. A senha padrão (`vinyl-local-password`) existe apenas para o ambiente local e pode ser substituída pela variável `VINYL_POSTGRES_PASSWORD` antes de iniciar o container.

Em seguida, configure o serviço para usar Marten. Os comandos abaixo gravam os valores no User Secrets do projeto da API, sem colocá-los no Git:

```bash
dotnet user-secrets set \
  "ConnectionStrings:Identity" \
  "Host=localhost;Port=5432;Database=vinyl_identity;Username=vinyl;Password=vinyl-local-password" \
  --project src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj

dotnet user-secrets set \
  "Persistence:Provider" \
  "marten" \
  --project src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj

dotnet user-secrets set \
  "Persistence:AutoCreateSchema" \
  "true" \
  --project src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj
```

Depois reinicie a API. Na primeira operação que acessar o repositório, o Marten criará o schema `vinyl_identity` e a tabela de documentos:

```bash
dotnet run --project src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj
```

Para voltar ao repositório em memória, sobrescreva apenas a configuração do provider:

```bash
dotnet user-secrets set \
  "Persistence:Provider" \
  "memory" \
  --project src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj
```

Para conectar a um User Pool e ao PostgreSQL, configure os valores por variáveis de ambiente, User Secrets ou pelo mecanismo de configuração do ambiente de deploy:

```bash
Authentication__Cognito__Authority=https://cognito-idp.sa-east-1.amazonaws.com/sa-east-1_example
Authentication__Cognito__UserPoolId=sa-east-1_example
Authentication__Cognito__UserInfoEndpoint=https://vinyl-dev.auth.sa-east-1.amazoncognito.com/oauth2/userInfo
Authentication__Cognito__AllowedClientIds__0=example-client-id
ConnectionStrings__Identity=Host=localhost;Port=5432;Database=vinyl_identity;Username=vinyl;Password=vinyl-local-password
Persistence__Provider=marten
Persistence__AutoCreateSchema=false
```

Não versionar valores de conexão, client secrets ou outros segredos. Em AWS, esses valores devem vir de Secrets Manager, Parameter Store ou do mecanismo seguro equivalente do ambiente de execução. Em produção, mantenha `Persistence:AutoCreateSchema=false` e adicione a execução controlada das migrações ao pipeline de deploy.

## Endpoints iniciais

- `GET /` - identificação do serviço;
- `GET /health/live` - liveness;
- `GET /health/ready` - readiness inicial;
- `GET /api/identity/me` - usuário autenticado e provisionado localmente;
- `GET /api/workspaces` - workspaces do usuário autenticado;
- `GET /api/workspaces/{workspaceId}` - workspace acessível pelo usuário autenticado;
- `POST /api/workspaces` - cria um workspace e atribui a role `Owner` ao usuário autenticado;
- `GET /api/workspaces/{workspaceId}/members` - lista os membros do workspace;
- `POST /api/workspaces/{workspaceId}/members` - adiciona um usuário local ao workspace;
- `PATCH /api/workspaces/{workspaceId}/members/{userId}` - altera a role do membro;
- `DELETE /api/workspaces/{workspaceId}/members/{userId}` - desativa o membership.

O endpoint de consulta de um workspace exige o header de contexto:

```http
X-Workspace-Id: WORKSPACE_ID
```

O valor do header deve ser igual ao `workspaceId` da rota. A role do usuário precisa conter a permissão `workspace.read`.

Os endpoints de membros exigem o mesmo header e as permissões `members.read` ou `members.manage`. A operação `DELETE` é uma desativação lógica. O último `Owner` ativo não pode ser removido nem rebaixado para outra role.

### Criar o primeiro workspace

Com um access token válido do Cognito:

```bash
curl -X POST http://localhost:5080/api/workspaces \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Meu Workspace"}'
```

Listar os workspaces:

```bash
curl http://localhost:5080/api/workspaces \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN"
```

Consultar um workspace:

```bash
curl http://localhost:5080/api/workspaces/WORKSPACE_ID \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" \
  -H "X-Workspace-Id: WORKSPACE_ID"
```

Listar membros:

```bash
curl http://localhost:5080/api/workspaces/WORKSPACE_ID/members \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" \
  -H "X-Workspace-Id: WORKSPACE_ID"
```

Adicionar um usuário local existente:

```bash
curl -X POST http://localhost:5080/api/workspaces/WORKSPACE_ID/members \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" \
  -H "X-Workspace-Id: WORKSPACE_ID" \
  -H "Content-Type: application/json" \
  -d '{"userId":"USER_ID","role":"Member"}'
```

Alterar a role de um membro:

```bash
curl -X PATCH http://localhost:5080/api/workspaces/WORKSPACE_ID/members/USER_ID \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" \
  -H "X-Workspace-Id: WORKSPACE_ID" \
  -H "Content-Type: application/json" \
  -d '{"role":"Admin"}'
```

Desativar um membro:

```bash
curl -X DELETE http://localhost:5080/api/workspaces/WORKSPACE_ID/members/USER_ID \
  -H "Authorization: Bearer SEU_ACCESS_TOKEN" \
  -H "X-Workspace-Id: WORKSPACE_ID"
```

No PostgreSQL com Marten, os documentos são armazenados inicialmente em:

- `vinyl_identity.mt_doc_user`;
- `vinyl_identity.mt_doc_workspace`;
- `vinyl_identity.mt_doc_membership`.
