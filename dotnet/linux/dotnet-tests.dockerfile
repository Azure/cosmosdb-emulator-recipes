FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine

WORKDIR /app/
COPY ./App/DotNet.Docker.csproj ./
COPY ./App/Program.cs ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

RUN apk add --no-cache bash
RUN apk add --no-cache curl

COPY ./entrypoint.sh ./entrypoint.sh

# convert line endings to unix
RUN dos2unix ./entrypoint.sh 
RUN chmod +x ./entrypoint.sh

ENTRYPOINT ["./entrypoint.sh", "azurecosmosemulator", "8081"]
