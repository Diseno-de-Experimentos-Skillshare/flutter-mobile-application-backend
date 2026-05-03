FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copiamos solo el proyecto y restauramos (Caché de capas)
# Si tienes varios proyectos, copia los .csproj correspondientes
COPY ["SkillShareBackend.csproj", "./"]
RUN dotnet restore

# 2. Ahora copiamos todo lo demás y compilamos
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 3. Copiamos los archivos publicados desde la etapa de build
COPY --from=build /app/publish .

# Configuración de puerto para Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SkillShareBackend.dll"]