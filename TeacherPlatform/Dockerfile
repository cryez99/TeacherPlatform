# Базовый образ для runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Установка EF Core tools
RUN dotnet tool install --global dotnet-ef --version 8.0.*

# Копируем ТОЛЬКО файлы проектов сначала
COPY ["TeacherPlatform/TeacherPlatform.csproj", "TeacherPlatform/"]
COPY ["TeacherPlatform.sln", "."]
RUN dotnet restore "TeacherPlatform/TeacherPlatform.csproj"

# 2. Копируем остальные файлы
COPY . .
WORKDIR "/src/TeacherPlatform"
RUN dotnet build "TeacherPlatform.csproj" -c Release -o /app/build

# Этап публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TeacherPlatform.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeacherPlatform.dll"]