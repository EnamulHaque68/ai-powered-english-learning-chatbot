# Multi-stage Dockerfile for ASP.NET Core 9.0 Web API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["backend/Chatbot.API/Chatbot.API.csproj", "backend/Chatbot.API/"]
RUN dotnet restore "backend/Chatbot.API/Chatbot.API.csproj"

# Copy source and publish
COPY . .
WORKDIR "/src/backend/Chatbot.API"
RUN dotnet publish "Chatbot.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Chatbot.API.dll"]
