#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["EmailSender/EmailSender.csproj", "EmailSender/"]
COPY ["ExternalLogic/ExternalLogic.csproj", "ExternalLogic/"]
RUN dotnet restore "EmailSender/EmailSender.csproj"
COPY . .
WORKDIR "/src/EmailSender"
RUN dotnet build "EmailSender.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailSender.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmailSender.dll"]