#! /bin/sh

# First parameter is build mode, defaults to Debug

MODE=${1:-Debug}
NAME="RabbitHarness"

if [ -x "$(command -v docker)" ]; then
  CONTAINER=$(docker run -d --rm -p 5672:5672 rabbitmq:3.6.11-management-alpine)
  echo "Started RabbitMQ container: $CONTAINER"
  sleep 2
fi

dotnet restore "$NAME.sln"
dotnet build "$NAME.sln" --configuration $MODE

find . -iname "*.Tests.csproj" -type f -exec dotnet test "{}" --configuration $MODE \;
dotnet pack ./src/$NAME --configuration $MODE --output ../../.build

if [ -x "$(command -v docker)" ]; then
  docker stop $CONTAINER
fi
