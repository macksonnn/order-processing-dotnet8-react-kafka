# Pedidos Santa Cruz

Challenge .NET 8 + React: pedido autenticado, persistência e processamento assíncrono.

## Como rodar

Infra:

```bash
docker compose up -d
```

Sobe Postgres (`5432`), Kafka (`9092`), Kafka UI (`http://localhost:8081`) e Keycloak (`http://localhost:8080`).

| Onde | URL | Usuário | Senha |
|---|---|---|---|
| App (realm `order-processing`) | `http://localhost:5173` | `demo` | `demo123` |
| App (mesmo realm) | `http://localhost:5173` | `admin` | `admin` |
| Console admin do Keycloak (realm `master`) | `http://localhost:8080` | `admin` | `admin` |

`demo` não entra no Administration Console. Se a tela for a do Keycloak admin, use `admin` / `admin`.

API:

```bash
dotnet run --project src/OrderProcessing.Api
```

- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health: `http://localhost:5080/health`

O DbUp aplica as migrations na subida. Se alguma falhar, a API não sobe.

Front:

```bash
cd frontend/order-processing-web
npm install
npm run dev
```

Testes:

```bash
dotnet test
```

## Fluxo

`POST /api/orders` grava pedido, itens e `OutboxMessage` **na mesma transação** e devolve `202` com `Pending`. Se o outbox falhar, o pedido também some.

O consumer abre uma TX curta (`Pending → Processing`), **fecha a conexão**, chama a integração fake (5–10s) e só então abre outra TX para `Completed` ou `Failed`. Sem lock de banco durante o delay.

Publisher e consumer sobem junto com a API (`BackgroundService`). Não deu tempo de separar um worker.

Login do pedido vem do `sub` do JWT. Sem roles.
