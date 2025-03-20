# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем файлы проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код и собираем проект
COPY . ./
RUN dotnet publish -c Release -o out

# Создаём финальный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Указываем порт, который будет использоваться
EXPOSE 8080

# Указываем переменную окружения для порта
ENV ASPNETCORE_URLS=http://+:8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "AgroMarket.Backend.dll"]