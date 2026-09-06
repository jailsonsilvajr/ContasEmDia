# backend/Api/Dockerfile
# Contexto de build: pasta "backend" (ver docker-compose.yml e workflow)

# Estágio "build": imagem com o SDK completo do .NET 10, usada só para
# compilar — não vai para a imagem final (fica bem maior que o runtime)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia primeiro só os arquivos .csproj de cada projeto...
COPY Api/ContasEmDia.Api.csproj Api/
COPY Application/ContasEmDia.Application.csproj Application/
COPY Domain/ContasEmDia.Domain.csproj Domain/
COPY Infrastructure/ContasEmDia.Infrastructure.csproj Infrastructure/

# ...e restaura as dependências NuGet nesta camada isolada — o Docker só
# reexecuta este passo (lento) se algum .csproj mudar
RUN dotnet restore Api/ContasEmDia.Api.csproj

# Só agora copia o restante do código-fonte de cada projeto
COPY Api/ Api/
COPY Application/ Application/
COPY Domain/ Domain/
COPY Infrastructure/ Infrastructure/

# Compila e publica a API em modo Release, reaproveitando o restore já feito
RUN dotnet publish Api/ContasEmDia.Api.csproj -c Release -o /app --no-restore

# Estágio "runtime": imagem menor, só com o runtime ASP.NET (sem SDK/build
# tools) — é essa que efetivamente vai para o GHCR e roda na VPS
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Copia apenas os artefatos já publicados do estágio de build
COPY --from=build /app .

# Faz o Kestrel escutar em todas as interfaces na porta 8080 dentro do container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Comando executado quando o container inicia
ENTRYPOINT ["dotnet", "ContasEmDia.Api.dll"]
