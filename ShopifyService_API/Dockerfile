# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#USER app
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80

# Ports expose
EXPOSE 80
EXPOSE 443


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ShopifyService_API/ShopifyService_API.csproj", "ShopifyService_API/"]
COPY ["Infrastructure/Common/PartFinderMicroServices_BusinessLogicLayer/PartFinderMicroServices_BusinessLogicLayer.csproj", "Infrastructure/Common/PartFinderMicroServices_BusinessLogicLayer/"]
COPY ["Infrastructure/Common/PartFinderMicroServices_DataAccessLayer/PartFinderMicroServices_DataAccessLayer.csproj", "Infrastructure/Common/PartFinderMicroServices_DataAccessLayer/"]
RUN dotnet restore "./ShopifyService_API/ShopifyService_API.csproj"
COPY . .
WORKDIR "/src/ShopifyService_API"
RUN dotnet build "./ShopifyService_API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ShopifyService_API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopifyService_API.dll"]
