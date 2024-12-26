

# Use the official .NET Core SDK as a parent image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the certificate to the container
COPY ./www.nehsa.net.pfx ./www.nehsa.net.pfx

# copy the application to the container
# COPY wwwroot /app/wwwroot

# Copy the project file and restore any dependencies (use .csproj for the project name)
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application code
COPY . .

# Publish the application
RUN dotnet publish -c Release

# we use -p 80:80/tcp -p 443:443/tcp in the docker run command
# EXPOSE 80/tcp 
# EXPOSE 443/tcp

# Start the application
# "dotnet nehsanet-app.dll --server.urls http://*/22007
ENTRYPOINT ["dotnet", "run"]