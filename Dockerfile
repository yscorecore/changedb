FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
RUN dotnet tool install ChangeDB.ConsoleApp --tool-path .
ENTRYPOINT ["./changedb"]