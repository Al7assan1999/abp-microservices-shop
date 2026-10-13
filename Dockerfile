# One image definition for every service: docker build --build-arg SERVICE=Catalog .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE
WORKDIR /src
COPY . .
RUN dotnet publish src/Shop.${SERVICE} -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG SERVICE
ENV SERVICE_DLL=Shop.${SERVICE}.dll \
    ASPNETCORE_URLS=http://+:8080
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet $SERVICE_DLL"]
