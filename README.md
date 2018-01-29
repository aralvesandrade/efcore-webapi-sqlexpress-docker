## Pré-requisitos

Precisa ter instalado os pacotes abaixo:

- [Docker](https://store.docker.com/editions/community/docker-ce-desktop-windows)
- [.NET Core 2.1.4 SDK](https://www.microsoft.com/net/download/thank-you/dotnet-sdk-2.1.4-windows-x64-installer)
- [Visual Code](https://go.microsoft.com/fwlink/?Linkid=852157)

## Criando o projeto .Net

A partir do terminal `Windows PowerShell` ou `Git Bash`, vamos criar um novo diretório do projeto e inicializar um novo projeto C# `webapi`:

```
$ mkdir dotnet-example
$ cd dotnet-example
$ dotnet new webapi
```

Em seguida, vamos restaurar e executar o nosso API:

```
$ dotnet restore
$ dotnet run
```

E, finalmente, em uma segunda janela terminal, vamos testar a API com `curl`:

```
$ curl http://localhost:5000/api/values
```

## Adicionando SQL Server

Agora é hora de adicionar um banco de dados. Graças a `Docker` e SQL Server para Linux, é super rápido e fácil de começar com isso. Do terminal, vamos baixar e executar uma nova instância do SQL Server como um container `Docker`.

```
$ docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=SqlExpress123' -p 1433:1433 --name sqlexpress -d microsoft/mssql-server-linux
```

Verificar se o container `Docker` do SQL Server está no ar

```
$ docker ps -a
```

## Adicionando Entity Framework ao projeto

Abrir o Visual Code via prompt, digitar o comando de dentro da pasta do projeto `dotnet-example`

```
$ code .
```

Utilizando o VsCode > View > Integrated Terminal você irá adicionar a `package` Microsoft SQL Server database provider for Entity Framework Core ao projeto

```
$ dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 2.0.1
```

## Criando e alterando arquivos

Criar arquivo na pasta Models > Product.cs

```
using System.ComponentModel.DataAnnotations;

namespace dotnet_example.Models  
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
```

Criar arquivo na pasta Models > ApiContext.cs

```
using Microsoft.EntityFrameworkCore;

namespace dotnet_example.Models  
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options)
            : base(options)
        {
            this.Database.EnsureCreated();
        }

        public DbSet<Product> Products { get; set; }
    }
}
```

Criar arquivo na pasta Controllers > ProductsController.cs

```
using System.Linq;  
using Microsoft.AspNetCore.Mvc;
using dotnet_example.Models;

namespace dotnet_example.Controllers  
{
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        private readonly ApiContext _context;

        public ProductsController(ApiContext context)
        {
            _context = context;
        }

        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            var model = _context.Products.ToList();

            return Ok(new { Products = model });
        }

        [HttpPost]
        public IActionResult Create([FromBody]Product model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Products.Add(model);
            _context.SaveChanges();

            return Ok(model);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody]Product model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = _context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            product.Name = model.Name;
            product.Price = model.Price;

            _context.SaveChanges();

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Remove(product);
            _context.SaveChanges();

            return Ok(product);
        }
    }
}
```

Atualizar `ConfigureServices` do arquivo Startup.cs

```
var hostname = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost";
var password = Environment.GetEnvironmentVariable("SQLSERVER_SA_PASSWORD") ?? "SqlExpress123";
var connString = $"Data Source={hostname};Initial Catalog=dotnet_example;User ID=sa;Password={password};";

services.AddDbContext<ApiContext>(options => options.UseSqlServer(connString));
```

Adicionar dependencias

```
using dotnet_example.Models;
using Microsoft.EntityFrameworkCore;
```

## Colocá-lo em Docker

Agora que temos o nosso serviço, precisamos obtê-lo em `Docker`. O primeiro passo é criar um novo `Dockerfile` que diz `Docker` como construir o nosso serviço. Crie um arquivo na pasta raiz chamada `Dockerfile` e adicione o seguinte conteúdo:

```
FROM microsoft/aspnetcore-build AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/aspnetcore
WORKDIR /app
COPY --from=build /app/out .
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

## Conectando Sql Server

Conectar no Sql Server via terminal:

```
$ docker exec -it sqlexpress /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P SqlExpress123
```

Exemplo de query:

```
1> use dotnet_example;
2> select * from products
3> go
```
