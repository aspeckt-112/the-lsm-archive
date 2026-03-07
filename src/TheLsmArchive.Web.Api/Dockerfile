FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["TheLsmArchive.slnx", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/TheLsmArchive.Web.Api/TheLsmArchive.Web.Api.csproj", "src/TheLsmArchive.Web.Api/"]
COPY ["src/TheLsmArchive.Models/TheLsmArchive.Models.csproj", "src/TheLsmArchive.Models/"]
COPY ["src/TheLsmArchive.Database/TheLsmArchive.Database.csproj", "src/TheLsmArchive.Database/"]
COPY ["src/TheLsmArchive.Common/TheLsmArchive.Common.csproj", "src/TheLsmArchive.Common/"]

# Restore packages
RUN dotnet restore "src/TheLsmArchive.Web.Api/TheLsmArchive.Web.Api.csproj"

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR "/src/src/TheLsmArchive.Web.Api"
RUN dotnet publish "TheLsmArchive.Web.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TheLsmArchive.Web.Api.dll"]
