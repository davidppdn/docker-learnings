# use the official .NET SDK image to build the application, hence the AS build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# set up the working directory inside the image so it is placed in a consistent place.
WORKDIR /api

# copy only the csproj file(s) and restore any dependencies (via NUGET)
# we do this because it is less volatile than the code itself, which allows us to
# cache the results of the restore step and avoid re-running it unless the csproj file(s) change.
COPY ["src/TaskManager.Api/TaskManager.Api.csproj", "TaskManager.Api/"]
RUN dotnet restore "TaskManager.Api/TaskManager.Api.csproj"

# copy the source code into the image and build the application
COPY ["src/TaskManager.Api/", "TaskManager.Api/"]
# publish using release configuration, specify output directory, skip restoring dependencies
# since we already restored them in the previous step.
RUN dotnet publish "TaskManager.Api/TaskManager.Api.csproj" \
    -c Release \
    -o /api/publish \
    --no-restore

# use the official ASP.NET image as the base image for the final stage of the build
# we do this because the only aspnet core is needed to run a publish application.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /api
COPY --from=build /api/publish .
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]