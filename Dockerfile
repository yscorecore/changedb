FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
ARG VERSION=1.0.12
ENV VERSION ${VERSION}
WORKDIR /app
RUN dotnet tool install ChangeDB.ConsoleApp --tool-path . --version ${VERSION}
ENTRYPOINT ["./changedb"]