FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

# copy everything else and build
COPY . ./
RUN dotnet publish FinnScrape.csproj -c Release -o output

# build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/output/ .
ENTRYPOINT ["dotnet", "FinnScrape"]	