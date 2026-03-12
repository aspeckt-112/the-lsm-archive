FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build
WORKDIR /src

RUN apt update \
    && apt install -y python3 \
    && dotnet workload install wasm-tools

# Copy project files
COPY ["TheLsmArchive.slnx", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/TheLsmArchive.Web.Frontend/TheLsmArchive.Web.Frontend.csproj", "src/TheLsmArchive.Web.Frontend/"]
COPY ["src/TheLsmArchive.ApiClient/TheLsmArchive.ApiClient.csproj", "src/TheLsmArchive.ApiClient/"]
COPY ["src/TheLsmArchive.Models/TheLsmArchive.Models.csproj", "src/TheLsmArchive.Models/"]
COPY ["src/TheLsmArchive.Common/TheLsmArchive.Common.csproj", "src/TheLsmArchive.Common/"]

RUN dotnet restore "src/TheLsmArchive.Web.Frontend/TheLsmArchive.Web.Frontend.csproj"

# Copy remaining source code
COPY . .
# Publish the WASM app
WORKDIR "/src/src/TheLsmArchive.Web.Frontend"
RUN dotnet publish "TheLsmArchive.Web.Frontend.csproj" -c Release --output /app

# Runtime stage
FROM nginx:latest
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY src/TheLsmArchive.Web.Frontend/nginx.conf /etc/nginx/nginx.conf
