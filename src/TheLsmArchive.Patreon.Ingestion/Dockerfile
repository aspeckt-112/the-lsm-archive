FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["TheLsmArchive.slnx", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/TheLsmArchive.Patreon.Ingestion/TheLsmArchive.Patreon.Ingestion.csproj", "src/TheLsmArchive.Patreon.Ingestion/"]
COPY ["src/TheLsmArchive.Models/TheLsmArchive.Models.csproj", "src/TheLsmArchive.Models/"]
COPY ["src/TheLsmArchive.Database/TheLsmArchive.Database.csproj", "src/TheLsmArchive.Database/"]
COPY ["src/TheLsmArchive.Common/TheLsmArchive.Common.csproj", "src/TheLsmArchive.Common/"]

# Restore packages
RUN dotnet restore "src/TheLsmArchive.Patreon.Ingestion/TheLsmArchive.Patreon.Ingestion.csproj"

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR "/src/src/TheLsmArchive.Patreon.Ingestion"
RUN dotnet publish "TheLsmArchive.Patreon.Ingestion.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TheLsmArchive.Patreon.Ingestion.dll"]
