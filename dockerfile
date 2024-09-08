
# Use the official .NET Core SDK as a parent image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore any dependencies (use .csproj for the project name)
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application code
COPY . .

# Publish the application
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/out ./

# Update package lists
RUN apt update && \
    apt upgrade -y

EXPOSE 22006/tcp

# Start the application
# "dotnet nehsanet-app.dll --server.urls http://*/22007
ENTRYPOINT ["dotnet", "nehsanet-web-app.dll","--server.urls","http://*/22006"]