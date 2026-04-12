FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CarvedRockFitness.Web.csproj", "."]
RUN dotnet restore "CarvedRockFitness.Web.csproj"
COPY . .
RUN dotnet publish "CarvedRockFitness.Web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN mkdir -p /app/wwwroot/uploads
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CarvedRockFitness.Web.dll"]
