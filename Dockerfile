# Use the official .NET 8 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the remaining source code and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official ASP.NET 8 runtime image for the final application
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Expose the port the application will run on
EXPOSE 8080

# Define the entry point for the application
# Make sure to replace "YourAppName.dll" with your actual DLL file name.
ENTRYPOINT ["dotnet", "pos_system_api.dll"]