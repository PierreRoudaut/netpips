# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
ARG VERSION=1.0.0
COPY ./ ./
# RUN dotnet restore
RUN dotnet publish netpips-api/Netpips.API -c Release -o out -p:Version=$VERSION

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Netpips.API.dll"]