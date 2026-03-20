# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EventCenter.Web/EventCenter.Web.csproj", "EventCenter.Web/"]
RUN dotnet restore "EventCenter.Web/EventCenter.Web.csproj"

COPY . .
WORKDIR "/src/EventCenter.Web"
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5270

ENTRYPOINT ["dotnet", "EventCenter.Web.dll"]
