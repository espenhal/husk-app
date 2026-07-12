.PHONY: help restore build run-api run-web docker-up docker-down docker-logs

help:
	@echo ""
	@echo "Husk"
	@echo ""
	@echo "Commands:"
	@echo "  make restore"
	@echo "  make build"
	@echo "  make run-api"
	@echo "  make run-web"
	@echo "  make docker-up"
	@echo "  make docker-down"
	@echo "  make docker-logs"
	@echo ""

restore:
	dotnet restore Husk.sln

build:
	dotnet build Husk.sln --no-restore

run-api:
	ASPNETCORE_URLS=http://localhost:5081 dotnet run --project src/Husk.Api/Husk.Api.csproj

run-web:
	ASPNETCORE_URLS=http://localhost:5080 HUSK_API_BASE_URL=http://localhost:5081 dotnet run --project src/Husk.Web/Husk.Web.csproj

docker-up:
	docker compose up -d --build

docker-down:
	docker compose down

docker-logs:
	docker compose logs --tail=100 -f
