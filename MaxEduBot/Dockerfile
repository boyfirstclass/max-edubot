# =========================
# STAGE 1: build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY MaxEduBot.csproj ./
RUN dotnet restore

# Копируем остальной код и публикуем
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# =========================
# STAGE 2: runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

# Kestrel будет слушать на 5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "MaxEduBot.dll"]
