FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CarvedRockFitness.Auth.csproj", "."]
RUN dotnet restore "CarvedRockFitness.Auth.csproj"
COPY . .
RUN dotnet publish "CarvedRockFitness.Auth.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN mkdir -p /certs
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CarvedRockFitness.Auth.dll"]
