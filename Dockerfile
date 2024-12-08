FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

WORKDIR /app

COPY . ./

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0

# TODO: add AWS env vars here

WORKDIR /app

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "Chefster.dll"]

# To build:
# docker build -t chefster .

# To run:
# docker run -d -p 5000:5144 chefster

# To see logs:
# docker logs *container id spit out by the docker run command*