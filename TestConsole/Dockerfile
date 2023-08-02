FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestConsole/TestConsole.csproj", "TestConsole/"]
RUN dotnet restore "TestConsole/TestConsole.csproj"
COPY . .
WORKDIR "/src/TestConsole"
RUN dotnet build "TestConsole.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestConsole.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG SECRET_SERVER_URL
ARG SECRET_SERVER_USERNAME
ARG SECRET_SERVER_PASSWORD
ARG SECRET_SERVER_SECRET_ID

ENV SECRET_SERVER_URL=$SECRET_SERVER_URL
ENV SECRET_SERVER_USERNAME=$SECRET_SERVER_USERNAME
ENV SECRET_SERVER_PASSWORD=$SECRET_SERVER_PASSWORD
ENV SECRET_SERVER_SECRET_ID=$SECRET_SERVER_SECRET_ID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestConsole.dll"]
