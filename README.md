## Criando o projeto .Net

A partir do terminal, vamos criar um novo diretório do projeto e inicializar um novo projeto C# `webapi`:

```
$ mkdir dotnet-example
$ cd dotnet-example
$ dotnet new webapi
```

Em seguida, vamos restaurar nossas dependências `NuGet` e executar o nosso API:

```
$ dotnet restore
$ dotnet run
```

E, finalmente, em uma segunda janela terminal, vamos testar a API com `curl`:

```
$ curl http://localhost:5000/api/values
```

## Adicionando SQL Server

Agora é hora de adicionar um banco de dados. Graças a `Docker` e SQL Server para Linux, é super rápido e fácil de começar com isso. Do terminal, vamos baixar e executar uma nova instância do SQL Server como um recipiente `Docker`.

```
$ docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=SqlExpress123' -p 1433:1433 --name sqlexpress -d microsoft/mssql-server-linux
```

## Adicionando Entity Framework ao projeto

Utilizando o VsCode > View > Integrated Terminal você irá adicionar a `package` Microsoft SQL Server database provider for Entity Framework Core ao projeto

```
$ dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 2.0.1
```

## Criando os arquivos

Models/Product.cs
Models/ApiContext.cs
Controllers/ProductsController.cs

## Atualizar ConfigureServices

```
var hostname = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost";
var password = Environment.GetEnvironmentVariable("SQLSERVER_SA_PASSWORD") ?? "SqlExpress123";
var connString = $"Data Source={hostname};Initial Catalog=dotnet_example;User ID=sa;Password={password};";

services.AddDbContext<ApiContext>(options => options.UseSqlServer(connString));
```

## Colocá-lo em Docker

Agora que temos o nosso serviço, precisamos obtê-lo em `Docker`. O primeiro passo é criar um novo `Dockerfile` que diz `Docker` como construir o nosso serviço. Crie um arquivo na pasta raiz chamada `Dockerfile` e adicione o seguinte conteúdo:

```
FROM microsoft/aspnetcore-build:2.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/aspnetcore:2.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "dotnet-example.dll"]
```

Em seguida, é preciso compilar o nosso aplicativo e usar a saída para construir uma imagem Docker com a tag `dotnet-example`:

```
$ docker build -t dotnet-example .
```

E, finalmente, podemos executar nosso novo recipiente, relacionando-a com o nosso recipiente SQL Server:

```
$ docker run -it --rm -p 5000:80 --link sqlexpress -e SQLSERVER_HOST=sqlexpress dotnet-example
```

## Testando a nossa API

Vamos usar `curl` para postar alguns dados para nossa API:

```
$ curl -i -H "Content-Type: application/json" -X POST -d '{"name": "6-Pack Beer", "price": "5.99"}' http://localhost:5000/api/products
```

Se tudo correr bem, você deve ver uma resposta 200 status, e nosso novo produto retornados como JSON (com um banco de dados id gerado adequada).

Em seguida, vamos modificar os nossos dados com um PUT e alterar o preço:

```
$ curl -i -H "Content-Type: application/json" -X PUT -d '{"name": "6-Pack Beer", "price": "7.99"}' http://localhost:5000/api/products/1
```

Claro, também podemos obter os nossos dados:

```
$ curl -i http://localhost:5000/api/products
```

E, finalmente, podemos excluí-lo:

```
$ curl -i -X DELETE http://localhost:5000/api/products/1
```

Esse projeto foi atualizado para AspNetCore 2.0 do artigo original http://blog.kontena.io/dot-net-core-and-sql-server-in-docker/# dotnet-example
