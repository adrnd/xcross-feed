# =========================
# 1. Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY ["xcross-backend/xcross-backend.csproj", "xcross-backend/"]
# Restore NuGet packages based on the .csproj file
RUN dotnet restore "xcross-backend/xcross-backend.csproj"

# Copy the rest of the application source code
COPY . .
WORKDIR "/src/xcross-backend"
RUN dotnet build "xcross-backend.csproj" -c Release -o /app/build

# =========================
# 2. Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .

# Expose port 8000 inside container
EXPOSE 8080



# Run the app
ENTRYPOINT ["dotnet", "xcross-backend.dll"]