FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . ./
RUN dotnet restore "WebApi/WebApi.csproj"
RUN dotnet publish "WebApi/WebApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 80
ENTRYPOINT ["dotnet", "WebApi.dll"]
