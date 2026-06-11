FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FlowLedger.sln ./
COPY FlowLedger.Api/FlowLedger.Api.csproj FlowLedger.Api/
COPY FlowLedger.Application/FlowLedger.Application.csproj FlowLedger.Application/
COPY FlowLedger.Domain/FlowLedger.Domain.csproj FlowLedger.Domain/
COPY FlowLedger.Infrastructure/FlowLedger.Infrastructure.csproj FlowLedger.Infrastructure/

RUN dotnet restore FlowLedger.Api/FlowLedger.Api.csproj

COPY . .
RUN dotnet publish FlowLedger.Api/FlowLedger.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FlowLedger.Api.dll"]
