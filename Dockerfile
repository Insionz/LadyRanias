# ---------- 1. Bygg React-appen ----------
FROM node:24 AS client
WORKDIR /client/ClientApp

COPY src/LadyRanias.App/ClientApp/package.json src/LadyRanias.App/ClientApp/package-lock.json ./
RUN npm ci

COPY src/LadyRanias.App/ClientApp/ ./
RUN npm run build
# vite.config.ts har outDir '../wwwroot', så filene havner i /client/wwwroot

# ---------- 2. Bygg .NET-appen ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/LadyRanias.App/LadyRanias.App.csproj src/LadyRanias.App/
RUN dotnet restore src/LadyRanias.App/LadyRanias.App.csproj -r linux-x64
COPY src/LadyRanias.App/ src/LadyRanias.App/
COPY --from=client /client/wwwroot src/LadyRanias.App/wwwroot
RUN dotnet publish src/LadyRanias.App/LadyRanias.App.csproj -c Release -o /app --no-restore -r linux-x64 --self-contained false -p:SkipClientBuild=true

# ---------- 3. Det ferdige imaget ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
WORKDIR /app

COPY --from=build /app .
ENTRYPOINT [ "dotnet", "LadyRanias.App.dll" ]