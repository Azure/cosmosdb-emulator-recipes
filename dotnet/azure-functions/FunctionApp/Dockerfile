#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FunctionApp.csproj", "."]
RUN dotnet restore "./FunctionApp.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "FunctionApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FunctionApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y dos2unix bash curl && rm -rf /var/lib/apt/lists/*
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY ./entrypoint.sh .
# convert line endings to unix
RUN dos2unix ./entrypoint.sh 
RUN chmod +x ./entrypoint.sh
ENTRYPOINT ["./entrypoint.sh"]
