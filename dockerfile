# Use the official .NET 6 SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory in the container
WORKDIR /app

# Copy the project files to the container
COPY . .

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 6 runtime image as the base image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory in the container
WORKDIR /app

# Copy the published application from the build image to the runtime image
COPY --from=build /app/out .

# Define the entry point for the application
ENTRYPOINT ["dotnet", "DiscNite.dll"]
