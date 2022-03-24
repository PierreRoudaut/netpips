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

# Filebot
RUN apt-get update
RUN apt-get install -y default-jre
RUN apt-get install -y default-jdk
RUN apt-get install -y libjna-java
RUN apt-get install -y wget
RUN wget -O filebot.deb 'https://app.filebot.net/download.php?type=deb&arch=amd64'
RUN dpkg -i filebot.deb
RUN rm -f filebot.deb
RUN filebot -version

# Mediainfo
RUN apt-get install -y mediainfo
RUN mediainfo --version

# Aria2c
RUN apt-get install -y aria2
RUN aria2c --version

# Transmission
RUN apt-get install -y transmission-cli
RUN transmission-remote --version

ENTRYPOINT ["dotnet", "Netpips.API.dll"]