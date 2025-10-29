# Estágio 1: Build (SDK)
# Usamos a imagem completa do SDK do .NET 10 para compilar o projeto
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia o arquivo .sln e os .csproj de CADA projeto PRIMEIRO
# Isso otimiza o cache do Docker. Se o código-fonte mudar, mas os projetos não,
# o 'dotnet restore' não precisa ser rodado novamente.
COPY ["JeevesMoney.sln", "."]
COPY ["JeevesMoney.Api/JeevesMoney.Api.csproj", "JeevesMoney.Api/"]
COPY ["JeevesMoney.Application/JeevesMoney.Application.csproj", "JeevesMoney.Application/"]
COPY ["JeevesMoney.Domain/JeevesMoney.Domain.csproj", "JeevesMoney.Domain/"]
COPY ["JeevesMoney.Infrastructure/JeevesMoney.Infrastructure.csproj", "JeevesMoney.Infrastructure/"]

# Restaura os pacotes (NuGet)
RUN dotnet restore "JeevesMoney.sln"

# Copia TODO o resto do código-fonte
COPY . .

# Publica a aplicação (API) em modo Release
# A saída será otimizada e salva em /app/publish
RUN dotnet publish "JeevesMoney.Api/JeevesMoney.Api.csproj" -c Release -o /app/publish

# ---

# Estágio 2: Final (Runtime)
# Usamos a imagem leve 'aspnet' que SÓ contém o necessário para rodar a API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# O Render define uma variável de ambiente $PORT. 
# Esta linha diz ao Kestrel (servidor do .NET) para escutar nessa porta.
# Se o $PORT não for definido, ele usará a porta 8080 como padrão.
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

# Expõe a porta para o Render (ele detecta isso)
EXPOSE 8080

# Copia os arquivos publicados do estágio de 'build'
COPY --from=build /app/publish .

# Ponto de entrada: o comando para rodar a sua API
ENTRYPOINT ["dotnet", "JeevesMoney.Api.dll"]