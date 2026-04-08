FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine As base

RUN apk add -u tzdata
ENV TZ='Asia/Kolkata'
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 As build
WORKDIR "/app"
COPY ["./TeleIcu-Live/TeleICU.API/TeleICU.API.csproj","."]
RUN dotnet restore "TeleICU.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TeleICU.API.csproj" -c Release "/app/build"

FROM build As publish
RUN dotnet publish "TeleICU.API.csproj" -c Release "/app/publish"

FROM base as final 
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet","TeleICU.API.dll"]
