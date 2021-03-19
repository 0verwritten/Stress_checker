# FROM mcr.microsoft.com/dotnet/aspnet:5.0

# WORKDIR /app
# COPY *.csproj ./
# RUN dotnet restore
# COPY . ./
# RUN dotnet publish -c Release -o out

# # FROM mcr.microsoft.com/dotnet/aspnet:5.0
# # WORKDIR /app
# # COPY --from=build-env /app/out .

# ENTRYPOINT ["dotnet", "telegram_stress_checker.dll"]

FROM mcr.microsoft.com/dotnet/sdk:3.1.407-buster-arm64v8 as build
# FROM microsoft/dotnet:3.1-sdk AS build-env

WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out -r linux-arm64

# FROM ubuntu:latest

FROM mcr.microsoft.com/dotnet/runtime:3.1-buster-slim-arm64v8

# COPY ./bin/Release/netcoreapp3.1/linux-arm64/publish .

WORKDIR /app

COPY --from=build /app/out .
COPY ./config.json .
COPY ./Data/stresses.txt ./Data/
# Copy ./Data/dbStructur

ENTRYPOINT ["./telegram_stress_checker"]
